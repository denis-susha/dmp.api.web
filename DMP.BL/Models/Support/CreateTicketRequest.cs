using DMP.BL.ModelValidation;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Support;

public class CreateTicketRequest : IValidate
{
    public string Subject { get; set; } = null!;
    public SupportTicketType Type { get; set; }
    public string Message { get; set; } = null!;
    public CreateTicketPayout? Payout { get; set; }

    public ValidationResult Validate()
    {
        if (Subject.Length is > 250 or < 5)
        {
            return ValidationResult.Fail("Subject is invalid");
        }

        if (Type == SupportTicketType.Payout)
        {
            if (Payout is null || !Payout.Validate().IsValid)
            {
                return ValidationResult.Fail("Payout is invalid");
            }
        }

        if (Message.Length > 10000)
        {
            return ValidationResult.Fail("Message is invalid");
        }

        return ValidationResult.Success();
    }
}
