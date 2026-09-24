using DMP.BL.ModelValidation;

namespace DMP.BL.Models.ProductCreation;

public class SetProductFileUploadedRequest : IValidate
{
    public int ProductId { get; set; }
    public int ProductFileId { get; set; }

    public ValidationResult Validate()
    {
        if (ProductId < 1)
        {
            return ValidationResult.Fail("ProductId is invalid");
        }

        if (ProductFileId < 1)
        {
            return ValidationResult.Fail("ProductFileId is invalid");
        }

        return ValidationResult.Success();
    }
}
