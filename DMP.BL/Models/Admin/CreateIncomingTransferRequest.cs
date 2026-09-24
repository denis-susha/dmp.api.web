using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Admin;

public class CreateIncomingTransferRequest : IValidate
{
    public string InvoiceId { get; set; } = null!;

    public ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(InvoiceId) || InvoiceId.Length != 22)
        {
            return ValidationResult.Fail("PayoutId is invalid");
        }

        return ValidationResult.Success();
    }
}
