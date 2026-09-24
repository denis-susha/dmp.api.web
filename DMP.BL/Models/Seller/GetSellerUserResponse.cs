using DMP.BL.Models.Auth;

namespace DMP.BL.Models.Seller;

public class GetSellerUserResponse
{
    public UserInfo? UserInfo { get; set; }
    public Store.Store? Store { get; set; }
}
