namespace DMP.BL.Models.Enumerations;

public enum CreateOrderStatus : byte
{
    Success = 0,
    OutdatedData = 50,
    InternalError = 51,
}
