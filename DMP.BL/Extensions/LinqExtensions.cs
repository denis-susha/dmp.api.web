namespace DMP.BL.Extensions;

public static class LinqExtensions
{
    public static bool In<T>(this T source, params T[] values) where T : struct, Enum => values.Contains(source);
}
