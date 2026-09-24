using DMP.BL.ModelValidation;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Support;

public class CreateTicketPayout : IValidate
{
    public string Address { get; set; } = null!;
    public Cryptocurrency Cryptocurrency { get; set; }
    public decimal Amount { get; set; }

    public ValidationResult Validate()
    {
        if (Address.Length is > 104 or < 25 || !RegExp.OnlyLatinAndDigitsRegex().IsMatch(Address))
        {
            return ValidationResult.Fail("Address is invalid");
        }

        if (Amount is < 0.000001m or > 1000000)
        {
            return ValidationResult.Fail("Amount is invalid");
        }

        return ValidationResult.Success();
    }
}
