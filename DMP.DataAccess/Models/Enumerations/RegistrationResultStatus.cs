namespace DMP.DataAccess.Models.Enumerations;

public enum RegistrationResultStatus : byte
{
    Success = 0,
    TurnstileVerificationFailed = 1,
    ErrorEmailIsNotUniq = 10,
}
