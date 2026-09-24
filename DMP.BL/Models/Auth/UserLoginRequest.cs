using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Auth;

public class UserLoginRequest : IValidate
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;

    public ValidationResult Validate()
    {
        if (Email.Length > 320 || !RegExp.EmailRegex().IsMatch(Email))
        {
            return ValidationResult.Fail("Email is invalid");
        }

        if (!RegExp.PasswordRegex().IsMatch(Password))
        {
            return ValidationResult.Fail("Password is invalid");
        }

        return ValidationResult.Success();
    }
}
