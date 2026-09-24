using System.Collections.Concurrent;
using DMP.BL.Constants;
using DMP.BL.Models.Finances;
using DMP.DataAccess;
using DMP.DataAccess.BillingModels.Enumerations;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMP.BL.Services;

public class FinancesService(
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IDbContextFactory<BillingDbContext> billingContextFactory,
    ILogger<FinancesService> logger,
    IHttpClientFactory httpClientFactory) : IFinancesService
{
    public async Task<List<AccountBalance>> GetAccountsBalances(Guid userId)
    {
        var accounts = await GetAccounts(userId);

        return accounts
            .Select(account => new AccountBalance { Cryptocurrency = account.Cryptocurrency, Balance = account.AccountBalance })
            .ToList();
    }

    public async Task<decimal> GetBalance(Guid userId)
    {
        var accounts = await GetAccounts(userId);
        var rates = await GetRates();

        return accounts.Sum(account => Math.Round(ExtractRate(account.Cryptocurrency, rates) * account.AccountBalance, 2));
    }

    public async Task<GetFinancesResponse> GetFinances(Guid userId)
    {
        var accounts = await GetAccounts(userId);
        var rates = await GetRates();

        foreach (var account in accounts)
        {
            account.Balance = Math.Round(ExtractRate(account.Cryptocurrency, rates) * account.AccountBalance, 2);
        }

        return new GetFinancesResponse
        {
            Accounts = accounts,
            Balance = accounts.Sum(a => a.Balance),
            TotalIncome = await GetTotalSalesSum(userId, rates)
        };
    }

    private async Task<decimal> GetTotalSalesSum(Guid userId, IReadOnlyDictionary<string, decimal> rates)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var salesSum = await context.SalesHeaders
            .Where(sh => sh.SellerId == userId)
            .AsNoTracking()
            .GroupBy(h => h.Cryptocurrency)
            .Select(g => new
            {
                Cryptocurrency = g.Key,
                TotalAmount = g.Sum(s => s.Amount)
            })
            .ToListAsync();

        return salesSum.Sum(sale => Math.Round(ExtractRate(sale.Cryptocurrency, rates) * sale.TotalAmount, 2));
    }

    private async Task<List<Account>> GetAccounts(Guid userId)
    {
        await using var context = await billingContextFactory.CreateDbContextAsync();
        return await context.Accounts
            .Where(a => a.UserId == userId && a.Status == AccountStatus.Active)
            .AsNoTracking()
            .Select(a => new Account
            {
                AccountId = a.AccountId,
                Cryptocurrency = a.Cryptocurrency,
                AccountBalance = a.AccountBalance
            })
            .ToListAsync();
    }

    private static decimal ExtractRate(Cryptocurrency cryptocurrency, IReadOnlyDictionary<string, decimal> rates) => cryptocurrency switch
    {
        Cryptocurrency.BTC => rates["btc"],
        Cryptocurrency.TRX => rates["trx"],
        Cryptocurrency.USDT_BEP20
            or Cryptocurrency.USDT_TRC20
            or Cryptocurrency.USDT_ERC20
            or Cryptocurrency.USDT_MATIC
            or Cryptocurrency.USDC_ERC20 => 1m,
        Cryptocurrency.BCH => rates["bch"],
        Cryptocurrency.BNB => rates["bnb"],
        Cryptocurrency.ETH => rates["eth"],
        Cryptocurrency.POL => rates["matic"],
        Cryptocurrency.XMR => rates["xmr"],
        Cryptocurrency.LTC => rates["ltc"],
        _ => throw new ArgumentOutOfRangeException(nameof(cryptocurrency), cryptocurrency, null)
    };

    private async Task<Dictionary<string, decimal>> GetRates()
    {
        var client = httpClientFactory.CreateClient("BitcartBackend");
        var rates = new ConcurrentBag<(string crypto, decimal rate)>();

        try
        {
            await Parallel.ForEachAsync(
                BlConstants.SupportedBcCryptos,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                async (crypto, cancellationToken) =>
                {
                    using var reply = await client.GetAsync($"cryptos/rate?currency={crypto}&fiat_currency=usd", cancellationToken);
                    var resultContent = await reply.Content.ReadAsStringAsync(cancellationToken);
                    rates.Add((crypto, Convert.ToDecimal(resultContent)));
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get crypto rates from BitcartBackend");
            throw;
        }

        return rates.ToDictionary(r => r.crypto, r => r.rate);
    }
}
