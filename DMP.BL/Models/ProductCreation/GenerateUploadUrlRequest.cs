using DMP.BL.ModelValidation;

namespace DMP.BL.Models.ProductCreation;

public class GenerateUploadUrlRequest : IValidate
{
    public string FileName { get; set; } = null!;
    public long FileSize { get; set; }
    public int ProductId { get; set; }

    public ValidationResult Validate()
    {
        if (ProductId < 1)
        {
            return ValidationResult.Fail("ProductId is invalid");
        }

        if (string.IsNullOrEmpty(FileName))
        {
            return ValidationResult.Fail("FileName is invalid");
        }

        if (FileSize < 1)
        {
            return ValidationResult.Fail("FileSize is invalid");
        }

        return ValidationResult.Success();
    }
}
