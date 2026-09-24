using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DMP.BL.Models.Auth;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Registration;
using Microsoft.IdentityModel.Tokens;

namespace DMP.BL.Services.User;

public class JwtTokenService : IJwtTokenService
{
    public string GenerateAccessToken(IEnumerable<Claim> claims, JwtSettings jwtSettings)
    {
        var expires = DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenExpirationMinutes);
        return GenerateToken(claims, jwtSettings.SecretKey, jwtSettings.Issuer, jwtSettings.Audience, expires);
    }

    public string GenerateRegistrationToken(IEnumerable<Claim> claims, JwtRegistrationSettings jwtSettings)
    {
        var expires = DateTime.UtcNow.AddMinutes(jwtSettings.TokenExpirationMinutes);
        return GenerateToken(claims, jwtSettings.SecretKey, jwtSettings.Issuer, jwtSettings.Audience, expires);
    }

    public string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public JwtTokenValidationResult ValidateRegistrationToken(string token, JwtRegistrationSettings settings)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ClockSkew = TimeSpan.Zero,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey))
        };

        var result = new JwtTokenValidationResult();

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out _);
            result.Claims = principal.Claims;
            result.Status = TokenValidationStatus.Success;
        }
        catch (SecurityTokenExpiredException ex)
        {
            result.Status = TokenValidationStatus.TokenExpired;
            result.Error = $"Token has expired. expiryDate={ex.Expires}";
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            result.Status = TokenValidationStatus.InvalidSignature;
            result.Error = "Invalid token signature";
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            result.Status = TokenValidationStatus.InvalidAudience;
            result.Error = $"Invalid audience. audience={ex.InvalidAudience}";
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            result.Status = TokenValidationStatus.InvalidIssuer;
            result.Error = $"Invalid issuer. issuer={ex.InvalidIssuer}";
        }
        catch (SecurityTokenNotYetValidException ex)
        {
            result.Status = TokenValidationStatus.TokenNotYetValid;
            result.Error = $"Token not valid yet. notBefore={ex.NotBefore}";
        }
        catch (SecurityTokenValidationException)
        {
            result.Status = TokenValidationStatus.ValidationError;
            result.Error = "Token validation error.";
        }
        catch (ArgumentException ex)
        {
            result.Status = TokenValidationStatus.ValidationError;
            result.Error = $"Token validation error. Exception={ex.Message}";
        }

        return result;
    }

    public DecodedToken DecodeToken(string? jwt)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        return new DecodedToken
        {
            Audiences = token.Audiences.ToList(),
            Claims = token.Claims.ToList(),
            ValidTo = token.ValidTo
        };
    }

    private static string GenerateToken(IEnumerable<Claim> claims, string secretKey, string issuer, string audience, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
