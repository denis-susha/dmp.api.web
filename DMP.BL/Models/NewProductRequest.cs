using DMP.BL.Models.ProductCreation;
using DMP.BL.ModelValidation;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models;

public class NewProductRequest : IValidate
{
    public int? ProductId { get; set; }
    public int? MenuCategoryId { get; set; }
    public bool UseCustomCategory { get; set; }
    public string? CustomCategory { get; set; }
    public ProductStatus? PreviousStatus { get; set; }
    public UserFeature UserFeatures { get; set; } = null!;
    public Dictionary<int, string>? Features { get; set; }
    public decimal Price { get; set; }
    public IEnumerable<NewProductImage>? ProductImages { get; set; }

    public ValidationResult Validate()
    {
        if (UserFeatures.Name.Length is > 200 or < 5 || !RegExp.ProductNameRegex().IsMatch(UserFeatures.Name))
        {
            return ValidationResult.Fail("Name is invalid");
        }

        if (UserFeatures.Description.Length is > 10000 or < 5)
        {
            return ValidationResult.Fail("Description is invalid");
        }

        if (UseCustomCategory)
        {
            if (string.IsNullOrEmpty(CustomCategory) || CustomCategory.Length is > 150 or < 1)
            {
                return ValidationResult.Fail("CustomCategory is invalid");
            }
        }
        else
        {
            if (!MenuCategoryId.HasValue)
            {
                if (string.IsNullOrEmpty(CustomCategory) || CustomCategory.Length is > 150 or < 1)
                {
                    return ValidationResult.Fail("MenuCategoryId is invalid");
                }
            }

            if (Features is not { Count: > 0 })
            {
                return ValidationResult.Fail("Features is invalid");
            }
        }

        if (Price is > 10000 or < 1)
        {
            return ValidationResult.Fail("Price is invalid");
        }

        return ValidationResult.Success();
    }
}
