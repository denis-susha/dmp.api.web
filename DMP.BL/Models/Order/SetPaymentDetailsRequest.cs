using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Order;

public class SetPaymentDetailsRequest : IValidate
{
    public string PaymentMethodId { get; set; } = null!;
    public int OrderId { get; set; }
    public string Address { get; set; } = null!;

    public ValidationResult Validate()
    {
        if (string.IsNullOrEmpty(PaymentMethodId))
        {
            return ValidationResult.Fail("PaymentMethodId is invalid");
        }

        if (string.IsNullOrEmpty(Address) || Address.Length < 10)
        {
            return ValidationResult.Fail("Address is invalid");
        }

        return ValidationResult.Success();
    }
}
