namespace DMP.DataAccess.BillingModels.Enumerations;

public enum TransactionType
{
    /// <summary>Basic transaction that accepts a payment from a buyer.</summary>
    Payment = 1,

    /// <summary>Cancels a previously received payment and refunds the money.</summary>
    Refund = 2,

    /// <summary>Moves funds to another account.</summary>
    Transfer = 3,

    /// <summary>Manual adjustment.</summary>
    Adjustment = 4,

    /// <summary>Moves funds to the seller's wallet.</summary>
    Payout = 5,

    /// <summary>Annual or monthly payment (subscription).</summary>
    RecurringPayment = 6,

    Fee = 7,
    MovePayment = 8,
}
