using DMP.BL.Models.Auth;
using DMP.BL.Models.Enumerations;

namespace DMP.BL.Models.User;

public class GetUserResponse
{
    public GetUserStatus Status { get; set; }
    public UserInfo UserInfo { get; set; } = null!;
}
