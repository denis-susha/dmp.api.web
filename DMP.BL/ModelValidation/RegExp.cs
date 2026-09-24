using System.Text.RegularExpressions;

namespace DMP.BL.ModelValidation;

public static partial class RegExp
{
    public const string EmailFormat = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    public const string PasswordFormat = @"^(?=.*\d)(?=.*[A-Z]).{8,32}$";
    public const string ProductName = @"^[\p{L}\d._\/+,\- ]+$";
    public const string NicknameIsValid = @"^[A-Za-z0-9_-]{3,200}$";

    [GeneratedRegex(EmailFormat)]
    public static partial Regex EmailRegex();

    [GeneratedRegex(PasswordFormat)]
    public static partial Regex PasswordRegex();

    [GeneratedRegex(ProductName)]
    public static partial Regex ProductNameRegex();

    [GeneratedRegex(NicknameIsValid)]
    public static partial Regex NicknameRegex();

    [GeneratedRegex(Constants.RegExps.OnlyLatinAndDigits)]
    public static partial Regex OnlyLatinAndDigitsRegex();
}
