namespace DMP.BL.Models.Registration;

public enum ConfirmEmailStatus : byte
{
    Success = 0,
    InvalidToken = 5,
    ExpiredToken = 6,
    UsedToken = 7,
}
