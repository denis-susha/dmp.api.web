namespace DMP.BL.ModelValidation;

public class ValidationResult
{
    public ValidationResult()
    {
    }

    public ValidationResult(bool isValid)
    {
        IsValid = isValid;
    }

    public bool IsValid { get; set; }
    public string? ErrorMsg { get; set; }

    public static ValidationResult Success() => new(true);

    public static ValidationResult Fail(string errorMsg) => new(false) { ErrorMsg = errorMsg };
}
