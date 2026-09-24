using DMP.BL.ModelValidation;

namespace DMP.BL.Models.ProductCreation;

public class SetProductImagesRequest : IValidate
{
    public int ProductId { get; set; }
    public NewProductImage[]? NewProductImages { get; set; }

    public ValidationResult Validate()
    {
        if (ProductId < 1)
        {
            return ValidationResult.Fail("ProductId is invalid");
        }

        if (NewProductImages is { Length: > 0 })
        {
            if (NewProductImages.Count(i => i.IsCover) != 1)
            {
                return ValidationResult.Fail("Invalid prop IsCover");
            }

            if (NewProductImages.GroupBy(i => i.Hash).Any(g => g.Count() > 1))
            {
                return ValidationResult.Fail("Invalid prop Hash");
            }
        }

        return ValidationResult.Success();
    }
}
