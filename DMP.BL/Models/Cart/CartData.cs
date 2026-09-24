namespace DMP.BL.Models.Cart;

public class CartData
{
    public ICollection<CartItem>? Items { get; set; }
    public ICollection<CartItem>? UnavailableItems { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
}
