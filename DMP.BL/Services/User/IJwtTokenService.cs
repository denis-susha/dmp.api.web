using System.Security.Claims;
using DMP.BL.Models.Auth;
using DMP.BL.Models.Registration;

namespace DMP.BL.Services.User;

public interface IJwtTokenService
{
    string GenerateAccessToken(IEnumerable<Claim> claims, JwtSettings jwtSettings);
    string GenerateRegistrationToken(IEnumerable<Claim> claims, JwtRegistrationSettings settings);
    string GenerateRefreshToken();
    DecodedToken DecodeToken(string? jwt);
    JwtTokenValidationResult ValidateRegistrationToken(string token, JwtRegistrationSettings settings);
}
