using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Admin;

public class CreatePayoutRequest : IValidate
{
    public string PayoutId { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal NetworkFee { get; set; }
    public string NetworkTransactionId { get; set; } = null!;

    public ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(PayoutId) || PayoutId.Length != 29)
        {
            return ValidationResult.Fail("PayoutId is invalid");
        }

        if (Amount < 1)
        {
            return ValidationResult.Fail("Amount is invalid");
        }

        if (NetworkTransactionId.Length is < 30 or > 300)
        {
            return ValidationResult.Fail("NetworkTransactionId is invalid");
        }

        return ValidationResult.Success();
    }
}
