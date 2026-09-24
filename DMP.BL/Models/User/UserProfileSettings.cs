using DMP.BL.ModelValidation;

namespace DMP.BL.Models.User;

public class UserProfileSettings : IValidate
{
    public string Nickname { get; set; } = null!;

    public ValidationResult Validate()
    {
        if (Nickname.Length is > 200 or < 3 || !RegExp.NicknameRegex().IsMatch(Nickname))
        {
            return ValidationResult.Fail("Nickname is invalid");
        }

        return ValidationResult.Success();
    }
}
