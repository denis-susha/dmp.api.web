using DMP.BL.Models;
using DMP.BL.Models.Cart;
using DMP.BL.Models.Enumerations;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class CartService(IDbContextFactory<DmpDbContext> dmpContextFactory, IProductService productService) : ICartService
{
    public async Task<CartData> GetCartData(Guid userId, bool isCheckout, string lng)
    {
        var result = new CartData
        {
            Items = [],
            UnavailableItems = []
        };

        List<CartItemDAL> cartItemsDal;
        await using (var context = await dmpContextFactory.CreateDbContextAsync())
        {
            cartItemsDal = (await context.CartItems.Where(c => c.UserId == userId && (!isCheckout || c.Selected)).AsNoTracking().ToListAsync())
                .OrderBy(c => c.CreatedAt).ToList();
        }

        if (cartItemsDal.Count == 0)
        {
            return result;
        }

        var products = await productService.GetProducts(cartItemsDal.Select(ci => ci.ProductId).ToArray());

        var totalQuantity = 0;
        var totalAmount = 0m;
        foreach (var cartItemDAL in cartItemsDal)
        {
            var product = products.First(p => p?.ProductId == cartItemDAL.ProductId)!;

            var itemAmount = product.Price * cartItemDAL.Quantity;

            var cartItem = new CartItem
            {
                ProductId = cartItemDAL.ProductId,
                Quantity = cartItemDAL.Quantity,
                Selected = cartItemDAL.Selected,
                Price = product.Price,
                Amount = itemAmount,
                Name = product.UserFeatures!.Name,
                Slug = product.Slug,
                ImgLink = product.ImgLinks is { Length: > 0 } ? product.ImgLinks[0] : null,
                FeaturesValues = await productService.GetProductFeaturesValues(product, lng),
            };

            if (product.Unlimited || cartItem.Quantity <= product.Quantity)
            {
                result.Items.Add(cartItem);

                if (cartItem.Selected)
                {
                    totalQuantity += cartItem.Quantity;
                    totalAmount += itemAmount;
                }
            }
            else
            {
                result.UnavailableItems.Add(cartItem);
            }
        }

        result.TotalAmount = totalAmount;
        result.TotalQuantity = totalQuantity;

        return result;
    }

    public async Task SelectCartItems(Guid userId, List<CartItemSelection> selections)
    {
        if (selections.Count == 0)
        {
            return;
        }

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        foreach (var selection in selections)
        {
            var cartItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == selection.ProductId);
            if (cartItem is not null)
            {
                cartItem.Selected = selection.Selected;
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<UpdateCartStatus> AddToCart(Guid userId, AddToCartRequest request)
    {
        // Note: the quantity already in the cart is not taken into account by this availability check.
        var product = await productService.GetProduct(request.ProductId);
        if (product is null || !CheckProductAvailableQty(product, request.Quantity))
        {
            return UpdateCartStatus.OutdatedData;
        }

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        if (request.OneClickBuying)
        {
            await context.CartItems.Where(ci => ci.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.Selected, false));
        }

        var cartItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == request.ProductId);
        if (cartItem is not null)
        {
            cartItem.Quantity = request.OneClickBuying ? request.Quantity : cartItem.Quantity + request.Quantity;
            cartItem.Selected = true;
        }
        else
        {
            context.CartItems.Add(new CartItemDAL
            {
                UserId = userId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                Selected = true
            });
        }

        await context.SaveChangesAsync();

        return UpdateCartStatus.Success;
    }

    public async Task<UpdateCartStatus> UpdateItem(Guid userId, UpdateCartItemRequest request)
    {
        var product = await productService.GetProduct(request.ProductId);
        if (product is null || !CheckProductAvailableQty(product, request.Quantity))
        {
            return UpdateCartStatus.OutdatedData;
        }

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var cartItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == request.ProductId);
        if (cartItem is null)
        {
            return UpdateCartStatus.OutdatedData;
        }

        cartItem.Quantity = request.Quantity;
        context.Update(cartItem);
        await context.SaveChangesAsync();

        return UpdateCartStatus.Success;
    }

    public async Task<UpdateCartStatus> RemoveItems(Guid userId, int[] productIds)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var cartItems = await context.CartItems.Where(ci => ci.UserId == userId && productIds.Contains(ci.ProductId)).ToListAsync();
        if (cartItems.Count > 0)
        {
            context.RemoveRange(cartItems);
            await context.SaveChangesAsync();
        }

        return UpdateCartStatus.Success;
    }

    private static bool CheckProductAvailableQty(Product product, int requestedQty) =>
        product.Unlimited || product.Quantity >= requestedQty;
}
