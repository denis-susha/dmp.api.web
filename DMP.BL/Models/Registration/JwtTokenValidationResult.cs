using System.Security.Claims;
using DMP.BL.Models.Enumerations;

namespace DMP.BL.Models.Registration;

public class JwtTokenValidationResult
{
    public TokenValidationStatus Status { get; set; }
    public IEnumerable<Claim> Claims { get; set; } = null!;
    public string Error { get; set; } = null!;
}
