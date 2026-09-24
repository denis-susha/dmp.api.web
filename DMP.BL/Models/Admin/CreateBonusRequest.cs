using DMP.BL.ModelValidation;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Admin;

public class CreateBonusRequest : IValidate
{
    public Guid TargetUserId { get; set; }
    public decimal Amount { get; set; }
    public Cryptocurrency Cryptocurrency { get; set; }
    public string? Comment { get; set; }

    public ValidationResult Validate()
    {
        if (Amount is < 0 or > 1000)
        {
            return ValidationResult.Fail("Amount is invalid");
        }

        return ValidationResult.Success();
    }
}
