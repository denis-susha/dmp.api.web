namespace DMP.BL.Models.Enumerations;

public enum TokenValidationStatus : byte
{
    Success = 0,
    TokenExpired,
    InvalidSignature,
    InvalidAudience,
    InvalidIssuer,
    TokenNotYetValid,
    ValidationError
}
