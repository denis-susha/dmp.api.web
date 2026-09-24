using DMP.BL.Models.Cart;
using DMP.BL.Models.Enumerations;

namespace DMP.BL.Services;

public interface ICartService
{
    Task<CartData> GetCartData(Guid userId, bool isCheckout, string lng);
    Task<UpdateCartStatus> AddToCart(Guid userId, AddToCartRequest request);
    Task<UpdateCartStatus> RemoveItems(Guid userId, int[] productIds);
    Task<UpdateCartStatus> UpdateItem(Guid userId, UpdateCartItemRequest request);
    Task SelectCartItems(Guid userId, List<CartItemSelection> selections);
}
