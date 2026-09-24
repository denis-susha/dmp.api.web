using System.Text;
using System.Text.RegularExpressions;

namespace DMP.BL.Helpers;

public static partial class SlugHelper
{
    private static readonly Dictionary<char, string> CyrillicToLatinMap = new()
    {
        { 'а', "a" }, { 'б', "b" }, { 'в', "v" }, { 'г', "g" }, { 'д', "d" },
        { 'е', "e" }, { 'ё', "yo" }, { 'ж', "zh" }, { 'з', "z" }, { 'и', "i" },
        { 'й', "y" }, { 'к', "k" }, { 'л', "l" }, { 'м', "m" }, { 'н', "n" },
        { 'о', "o" }, { 'п', "p" }, { 'р', "r" }, { 'с', "s" }, { 'т', "t" },
        { 'у', "u" }, { 'ф', "f" }, { 'х', "h" }, { 'ц', "ts" }, { 'ч', "ch" },
        { 'ш', "sh" }, { 'щ', "sch" }, { 'ъ', "" }, { 'ы', "y" }, { 'ь', "" },
        { 'э', "e" }, { 'ю', "yu" }, { 'я', "ya" },

        { 'А', "A" }, { 'Б', "B" }, { 'В', "V" }, { 'Г', "G" }, { 'Д', "D" },
        { 'Е', "E" }, { 'Ё', "Yo" }, { 'Ж', "Zh" }, { 'З', "Z" }, { 'И', "I" },
        { 'Й', "Y" }, { 'К', "K" }, { 'Л', "L" }, { 'М', "M" }, { 'Н', "N" },
        { 'О', "O" }, { 'П', "P" }, { 'Р', "R" }, { 'С', "S" }, { 'Т', "T" },
        { 'У', "U" }, { 'Ф', "F" }, { 'Х', "H" }, { 'Ц', "Ts" }, { 'Ч', "Ch" },
        { 'Ш', "Sh" }, { 'Щ', "Sch" }, { 'Ъ', "" }, { 'Ы', "Y" }, { 'Ь', "" },
        { 'Э', "E" }, { 'Ю', "Yu" }, { 'Я', "Ya" }
    };

    public static string ToSlug(string phrase)
    {
        if (string.IsNullOrEmpty(phrase))
        {
            return string.Empty;
        }

        var transliterated = new StringBuilder(phrase.Length);
        foreach (var c in phrase)
        {
            if (CyrillicToLatinMap.TryGetValue(c, out var latin))
            {
                transliterated.Append(latin);
            }
            else
            {
                transliterated.Append(c);
            }
        }

        var slug = transliterated.ToString().ToLowerInvariant();

        // Keep only latin letters, digits, whitespace and hyphens.
        slug = InvalidCharsRegex().Replace(slug, "");

        // Collapse whitespace and hyphen runs into a single hyphen.
        return SeparatorsRegex().Replace(slug, "-").Trim('-');
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex SeparatorsRegex();
}
