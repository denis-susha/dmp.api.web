using DMP.BL.Models.Enumerations;

namespace DMP.BL.Models.Cart;

public class UpdateCartItemResponse
{
    public UpdateCartStatus Status { get; set; }
    public CartData? CartData { get; set; }
}
