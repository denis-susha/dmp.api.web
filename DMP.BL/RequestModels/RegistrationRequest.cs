using DMP.BL.ModelValidation;

namespace DMP.BL.RequestModels;

public class RegistrationRequest : IValidate
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public string TurnstileToken { get; set; } = null!;

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

        if (!string.Equals(Password, ConfirmPassword))
        {
            return ValidationResult.Fail("ConfirmPassword is invalid");
        }

        if (string.IsNullOrWhiteSpace(TurnstileToken))
        {
            return ValidationResult.Fail("TurnstileToken is invalid");
        }

        return ValidationResult.Success();
    }
}
