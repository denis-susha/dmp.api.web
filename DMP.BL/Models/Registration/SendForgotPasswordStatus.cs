namespace DMP.BL.Models.Registration;

public enum SendForgotPasswordStatus : byte
{
    Success = 0,
    InvalidEmail,
    TooManyAttempts
}
