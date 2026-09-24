using DMP.BL.Models.Auth;

namespace DMP.BL.Models.Registration;

public class ConfirmEmailResult : UserAuthTokens
{
    public ConfirmEmailResponse Response { get; set; } = null!;
}
