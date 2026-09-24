using System.Security.Claims;

namespace DMP.BL.Models.Registration;

public class DecodedToken
{
    public List<string> Audiences { get; set; } = null!;
    public List<Claim>? Claims { get; set; }
    public DateTime ValidTo { get; set; }
}
