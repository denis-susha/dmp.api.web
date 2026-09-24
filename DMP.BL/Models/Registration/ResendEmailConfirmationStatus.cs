namespace DMP.BL.Models.Registration;

public enum ResendEmailConfirmationStatus : byte
{
    Success = 0,
    InvalidEmail,
    EmailIsVerified,
    TooManyAttempts
}
