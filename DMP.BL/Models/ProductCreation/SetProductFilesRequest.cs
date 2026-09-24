using DMP.BL.ModelValidation;

namespace DMP.BL.Models.ProductCreation;

public class SetProductFilesRequest : IValidate
{
    public int ProductId { get; set; }
    public int[]? ProductFileIds { get; set; }
    public bool IsUnlimited { get; set; }
    public int? Quantity { get; set; }
    public bool IsLines { get; set; }
    public string[]? Lines { get; set; }

    public ValidationResult Validate()
    {
        if (ProductId < 1)
        {
            return ValidationResult.Fail("ProductId is invalid");
        }

        if (IsLines)
        {
            if (Lines is not { Length: > 0 })
            {
                return ValidationResult.Fail("Lines is invalid");
            }

            var lines = Lines.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            var duplicates = lines.GroupBy(l => l).Any(g => g.Count() > 1);
            var lengthIsInvalid = lines.Any(l => l.Length is < 1 or > 600);

            if (lines.Length == 0 || duplicates || lines.Length > 1000 || lengthIsInvalid)
            {
                return ValidationResult.Fail("Lines is invalid");
            }
        }
        else
        {
            if (ProductFileIds is not { Length: > 0 })
            {
                return ValidationResult.Fail("ProductFileIds is invalid");
            }
        }

        if (!IsLines && !IsUnlimited && Quantity is null or < 1)
        {
            return ValidationResult.Fail("Quantity is invalid");
        }

        return ValidationResult.Success();
    }
}
