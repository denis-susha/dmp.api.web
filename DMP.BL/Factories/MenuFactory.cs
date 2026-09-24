using DMP.BL.Models;
using DMP.DataAccess.Models;

namespace DMP.BL.Factories;

public class MenuFactory : IMenuFactory
{
    public Menu ConvertMenuToBL(MenuDAL menuDAL) => new()
    {
        MenuId = menuDAL.MenuId,
        Categories = menuDAL.Categories?.Select(ConvertMenuCategoryToBL)
    };

    public MenuCategory ConvertMenuCategoryToBL(MenuCategoryDAL menuCategoryDAL) => new()
    {
        MenuCategoryId = menuCategoryDAL.MenuCategoryId,
        MenuId = menuCategoryDAL.MenuId,
        Name = menuCategoryDAL.Name,
        ParentId = menuCategoryDAL.ParentId,
        Order = menuCategoryDAL.Order,
        Slug = menuCategoryDAL.Slug,
        Url = $"category/{menuCategoryDAL.Slug}-{menuCategoryDAL.MenuCategoryId}",
        Children = menuCategoryDAL.Children?.Select(ConvertMenuCategoryToBL)
    };

    public ProductCategory ConvertMenuCategoryToProductCategoryBL(MenuCategoryDAL menuCategoryDAL) => new()
    {
        CategoryId = menuCategoryDAL.MenuCategoryId,
        Name = menuCategoryDAL.Name,
        Order = menuCategoryDAL.Order,
        Children = menuCategoryDAL.Children?.Select(ConvertMenuCategoryToProductCategoryBL)
    };
}
