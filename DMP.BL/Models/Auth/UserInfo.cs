using DMP.BL.Models.Enumerations;

namespace DMP.BL.Models.Auth;

public class UserInfo
{
    public bool IsClient { get; set; }
    public UserRole Role { get; set; }
    public bool HideSellerStart { get; set; }
    public string Name { get; set; } = null!;
}
