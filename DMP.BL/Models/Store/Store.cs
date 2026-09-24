using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Store;

public class Store : IValidate
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = null!;
    public string? CoverPath { get; set; }

    public ValidationResult Validate()
    {
        if (Name.Length is < 2 or > 100)
        {
            return ValidationResult.Fail("Name is invalid");
        }

        return ValidationResult.Success();
    }
}
