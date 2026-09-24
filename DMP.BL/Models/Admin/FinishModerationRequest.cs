using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Admin;

public class FinishModerationRequest : IValidate
{
    public int ProductId { get; set; }
    public FinishModerationStatus Status { get; set; }
    public string? MessageForSeller { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public int? NewMenuCategoryId { get; set; }

    public ValidationResult Validate()
    {
        if (ProductId < 1)
        {
            return ValidationResult.Fail("ProductId is invalid");
        }

        if (Status != FinishModerationStatus.Ready && string.IsNullOrEmpty(MessageForSeller))
        {
            return ValidationResult.Fail("Invalid request");
        }

        return ValidationResult.Success();
    }
}
