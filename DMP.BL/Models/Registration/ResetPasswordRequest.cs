using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Registration;

public class ResetPasswordRequest : IValidate
{
    public string Token { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;

    public ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            return ValidationResult.Fail("Token is invalid");
        }

        if (!RegExp.PasswordRegex().IsMatch(Password))
        {
            return ValidationResult.Fail("Password is invalid");
        }

        if (!string.Equals(Password, ConfirmPassword))
        {
            return ValidationResult.Fail("ConfirmPassword is invalid");
        }

        return ValidationResult.Success();
    }
}
