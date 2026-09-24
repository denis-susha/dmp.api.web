using DMP.BL.Models;
using DMP.DataAccess.Models;

namespace DMP.BL.Factories;

public interface IMenuFactory
{
    Menu ConvertMenuToBL(MenuDAL menuDAL);
    MenuCategory ConvertMenuCategoryToBL(MenuCategoryDAL menuCategoryDAL);
    ProductCategory ConvertMenuCategoryToProductCategoryBL(MenuCategoryDAL menuCategoryDAL);
}
