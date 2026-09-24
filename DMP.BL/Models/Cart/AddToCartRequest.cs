using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Cart;

public class AddToCartRequest : IValidate
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public bool OneClickBuying { get; set; }

    public ValidationResult Validate()
    {
        if (Quantity <= 0)
        {
            return ValidationResult.Fail("Quantity is invalid");
        }

        return ValidationResult.Success();
    }
}
