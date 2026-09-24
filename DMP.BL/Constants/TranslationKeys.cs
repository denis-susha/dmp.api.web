namespace DMP.BL.Constants;

internal static class TranslationKeys
{
    private const string MenuKey = "menu.{0}.{1}";
    private const string MenuCategoryKey = "menucategory.{0}.{1}";

    public const string FeatureKey = "feature.{0}.{1}";
    public const string FeatureItemKey = "featureitem.{0}.{1}";

    public static string GetMenuKey(bool isCategory, int id, string name) =>
        string.Format(isCategory ? MenuCategoryKey : MenuKey, id, name);
}
