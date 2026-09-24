namespace DMP.DataAccess.Models.Enumerations;

[Flags]
public enum UserFlags
{
    None = 0,
    IsClient = 1,
    IsSeller = 2,
    IsAdmin = 4,
    HideSellerStart = 8,
}
