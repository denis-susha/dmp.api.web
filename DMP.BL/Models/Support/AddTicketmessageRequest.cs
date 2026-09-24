using DMP.BL.ModelValidation;

namespace DMP.BL.Models.Support;

public class AddTicketMessageRequest : IValidate
{
    public int SupportTicketId { get; set; }
    public string Message { get; set; } = null!;

    public ValidationResult Validate()
    {
        if (SupportTicketId < 1)
        {
            return ValidationResult.Fail("SupportTicketId is invalid");
        }

        if (Message.Length > 10000)
        {
            return ValidationResult.Fail("Message is invalid");
        }

        return ValidationResult.Success();
    }
}
