using DMP.DataAccess.BillingModels.Enumerations;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.BillingModels;

public class GeneralLedgerDAL
{
    public int GeneralLedgerId { get; set; }

    public string TransactionId { get; set; } = null!;

    /// <summary>The date the transaction occurred.</summary>
    public DateTimeOffset Date { get; set; }

    public int AccountId { get; set; }

    public string? Description { get; set; }

    /// <summary>The amount debited to the source account.</summary>
    public decimal DebitAmount { get; set; }

    /// <summary>The amount credited to the target account.</summary>
    public decimal CreditAmount { get; set; }

    public TransactionType TransactionType { get; set; }
    public Currency Currency { get; set; }
    public TransactionSourceType TransactionSource { get; set; }

    /// <summary>The user or system process that created the transaction.</summary>
    public Guid? CreatedBy { get; set; }

    public string? ReferenceNumber { get; set; }
    public decimal? Rate { get; set; }
}
