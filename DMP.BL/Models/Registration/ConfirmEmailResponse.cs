using DMP.BL.Models.Auth;

namespace DMP.BL.Models.Registration;

public class ConfirmEmailResponse
{
    public AuthInfo AuthInfo { get; set; } = null!;
    public ConfirmEmailStatus Status { get; set; }
}
