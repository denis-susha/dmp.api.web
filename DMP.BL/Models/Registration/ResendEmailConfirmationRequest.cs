using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Registration;

public class ResendEmailConfirmationRequest : IValidate
{
    public bool IsClient { get; set; }
    public string Email { get; set; } = null!;

    public ValidationResult Validate()
    {
        if (Email.Length > 320 || !RegExp.EmailRegex().IsMatch(Email))
        {
            return ValidationResult.Fail("Email is invalid");
        }

        return ValidationResult.Success();
    }
}
