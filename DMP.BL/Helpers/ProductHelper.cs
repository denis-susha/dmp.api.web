using DMP.DataAccess.Models;

namespace DMP.BL.Helpers;

public static class ProductHelper
{
    public static bool CheckProductAvailableQty(this ProductDAL product, int requestedQty) =>
        product.Unlimited || product.Quantity >= requestedQty;
}
