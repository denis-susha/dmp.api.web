using DMP.DataAccess.BillingModels.Enumerations;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.BillingModels;

public class TransactionDAL
{
    public string TransactionId { get; set; } = null!;

    public int? OrderId { get; set; }

    public DateTimeOffset? TransactionDate { get; set; }

    public TransactionType TransactionType { get; set; }

    /// <summary>Price in the main currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>Main currency.</summary>
    public Currency Currency { get; set; }

    /// <summary>
    /// The account owner involved in the transaction (sender or receiver, depending on the nature of the transaction).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>Target account.</summary>
    public int AccountId { get; set; }

    public TransactionStatus Status { get; set; }

    /// <summary>Bitcart payment method id.</summary>
    public string? PaymentMethodId { get; set; }

    /// <summary>Bitcart invoice id.</summary>
    public string? ReferenceNumber { get; set; }

    public string? Description { get; set; }

    /// <summary>Seller id, if applicable.</summary>
    public Guid? SellerId { get; set; }

    /// <summary>The fee charged for processing the transaction, if applicable.</summary>
    public decimal FeeAmount { get; set; }

    /// <summary>The final amount after subtracting fees from the total transaction amount.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>The system or module where the transaction originated (e.g. web app, mobile app, API).</summary>
    public TransactionSourceType TransactionSource { get; set; }

    public string? SourceTransactionId { get; set; }

    /// <summary>
    /// Links to the original transaction when this one is part of a chain (e.g. a refund of a prior payment).
    /// </summary>
    public string? RelatedTransactionId { get; set; }

    /// <summary>When the transaction record was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the transaction record was last updated (e.g. status change).</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>The IP address the transaction was initiated from (useful for fraud detection).</summary>
    public string? IPAddress { get; set; }

    /// <summary>Exchange rate between the main currency and the paid currency.</summary>
    public decimal? Rate { get; set; }

    public int? OrderLineId { get; set; }

    /// <summary>The invoice price.</summary>
    public decimal Price { get; set; }

    /// <summary>The invoice price currency.</summary>
    public Currency PriceCurrency { get; set; }
}
