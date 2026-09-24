namespace DMP.BL.Models.Finances;

public class GetFinancesResponse
{
    public List<Account> Accounts { get; set; } = null!;
    public decimal Balance { get; set; }
    public decimal TotalIncome { get; set; }
}
