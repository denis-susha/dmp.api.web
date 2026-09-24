using System.Security.Claims;
using System.Text.Json;
using DMP.BL.Constants;
using DMP.BL.Helpers;
using DMP.BL.Models;
using DMP.BL.Models.Auth;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Mail;
using DMP.BL.Models.Registration;
using DMP.BL.RequestModels;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMP.BL.Services.User;

public class RegistrationService(
    ILogger<RegistrationService> logger,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IJwtTokenService jwtTokenService,
    IMailService mailService,
    IApplicationSettingsService applicationSettingsService,
    IAuthService authService,
    IUserService userService,
    IRedisService redisService,
    IHttpClientFactory httpClientFactory) : IRegistrationService
{
    private const string TurnstileVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    private const int MaxUserNameLength = 200;

    private string LogoUrl => $"{applicationSettingsService.MinioSettings.S3PublicEndpoint}/{BlConstants.LogoPath}";

    public Task<RegistrationResultStatus> RegisterSeller(RegistrationRequest request, string lng, string? remoteIp) =>
        Register(request, lng, remoteIp, isSeller: true);

    public Task<RegistrationResultStatus> RegisterClient(RegistrationRequest request, string lng, string? remoteIp) =>
        Register(request, lng, remoteIp, isSeller: false);

    private async Task<RegistrationResultStatus> Register(RegistrationRequest request, string lng, string? remoteIp, bool isSeller)
    {
        var email = EmailHelper.Normalize(request.Email);
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        if (await context.Users.AnyAsync(u => u.Email.ToLower() == email))
        {
            logger.LogInformation("Email {Email} exists in db.", email);

            return RegistrationResultStatus.ErrorEmailIsNotUniq;
        }

        if (!await VerifyTurnstile(request.TurnstileToken, remoteIp))
        {
            return RegistrationResultStatus.TurnstileVerificationFailed;
        }

        var language = LocaleHelper.ConvertLocaleToLanguage(lng);
        var (hash, salt) = PasswordHelper.HashPassword(request.Password);

        var newUser = new UserDAL
        {
            UserId = Guid.NewGuid(),
            Email = email,
            Password = hash,
            Salt = salt,
            Status = UserStatus.Unverified,
            Flags = isSeller ? UserFlags.IsSeller : UserFlags.IsClient,
            Language = language,
            Name = ExtractUserNameFromEmail(request.Email.Trim())
        };

        context.Users.Add(newUser);

        var emailConfirmation = new EmailConfirmationDAL
        {
            UserId = newUser.UserId,
            Token = GenerateRegistrationToken(newUser.UserId),
            SentAt = DateTimeOffset.UtcNow,
            Type = EmailConfirmationType.ConfirmEmail
        };

        context.EmailConfirmations.Add(emailConfirmation);

        if (isSeller)
        {
            context.Stores.Add(new StoreDAL
            {
                SellerId = newUser.UserId,
                Name = newUser.Name,
            });
        }

        await context.SaveChangesAsync();

        if (isSeller)
        {
            await userService.CreateBillingAccounts(newUser.UserId);
        }

        var appHost = isSeller ? applicationSettingsService.DmpHostsSettings.Seller : applicationSettingsService.DmpHostsSettings.Client;
        await mailService.CreateConfirmEmailRegistrationMail(
            CreateConfirmationMailModel($"{appHost}/registration/confirm-email/{emailConfirmation.Token}"),
            newUser.Email,
            language);
        await SendSysNotify(newUser.Email, newUser.UserId);

        return RegistrationResultStatus.Success;
    }

    private async Task SendSysNotify(string email, Guid userId)
    {
        try
        {
            await redisService.AddSysNotification($"New user registration.\nEmail: {email}\nUserId: {userId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send system notification for new user {UserId}", userId);
        }
    }

    private async Task<bool> VerifyTurnstile(string token, string? remoteIp)
    {
        var client = httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "secret", applicationSettingsService.CfSettings.TURNSTILE_SECRET_KEY },
            { "response", token },
            { "remoteip", remoteIp ?? "" }
        });
        using var response = await client.PostAsync(TurnstileVerifyUrl, content);

        var resultContent = await response.Content.ReadAsStringAsync();
        var cfTurnstileResponse = JsonSerializer.Deserialize<CfTurnstileResponse>(resultContent)!;

        return cfTurnstileResponse.Success;
    }

    private static string ExtractUserNameFromEmail(string email)
    {
        var namePart = email.Split('@')[0];

        return namePart.Length <= MaxUserNameLength ? namePart : namePart[..MaxUserNameLength];
    }

    public async Task<ConfirmEmailResult> ConfirmEmail(string token)
    {
        var result = new ConfirmEmailResult
        {
            Response = new ConfirmEmailResponse()
        };

        var validationRes = jwtTokenService.ValidateRegistrationToken(token, applicationSettingsService.JwtRegistrationSettings);
        var resultStatus = ConvertTokenStatusToConfirmEmailStatus(validationRes.Status);

        if (resultStatus != ConfirmEmailStatus.Success)
        {
            logger.LogInformation("Registration token validation failed: {Error}.", validationRes.Error);
            result.Response.Status = resultStatus;
            return result;
        }

        var nameIdentifierClaim = validationRes.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (nameIdentifierClaim is null || !Guid.TryParse(nameIdentifierClaim.Value, out var userId))
        {
            logger.LogInformation("Registration token has an invalid NameIdentifier claim.");
            result.Response.Status = ConfirmEmailStatus.InvalidToken;
            return result;
        }

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var emailConfirmation = await context.EmailConfirmations.FirstOrDefaultAsync(ec =>
            ec.UserId == userId && ec.Token == token && ec.Type == EmailConfirmationType.ConfirmEmail);

        if (emailConfirmation is null)
        {
            logger.LogInformation("Registration token for user {UserId} was not found.", userId);
            result.Response.Status = ConfirmEmailStatus.InvalidToken;
            return result;
        }

        if (emailConfirmation.Used)
        {
            logger.LogInformation("Registration token for user {UserId} has already been used.", userId);
            result.Response.Status = ConfirmEmailStatus.UsedToken;
            return result;
        }

        var user = await context.Users.FirstAsync(u => u.UserId == userId);
        user.Status = UserStatus.Active;

        emailConfirmation.Used = true;
        context.Update(emailConfirmation);

        await context.SaveChangesAsync();

        var mailModel = new MailRegistration
        {
            ButtonLink = user.Flags.HasFlag(UserFlags.IsSeller)
                ? applicationSettingsService.DmpHostsSettings.Seller
                : applicationSettingsService.DmpHostsSettings.Client,
            UserName = user.Name,
            LogoUrl = LogoUrl,
            Year = DateTime.UtcNow.Year.ToString()
        };

        await mailService.CreateRegistrationMail(mailModel, user.Email, user.Language);

        var userAuthTokens = await authService.GenerateTokens(user);

        result.AccessToken = userAuthTokens.AccessToken;
        result.RefreshToken = userAuthTokens.RefreshToken;
        result.Response.AuthInfo = new AuthInfo
        {
            UserInfo = userService.GetUserInfo(user),
            Expiry = userAuthTokens.RefreshTokenExpiry
        };

        return result;
    }

    public async Task<ResendEmailConfirmationStatus> ResendEmailConfirmation(ResendEmailConfirmationRequest request, string lng)
    {
        var emailValue = EmailHelper.Normalize(request.Email);
        var language = LocaleHelper.ConvertLocaleToLanguage(lng);
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == emailValue);

        if (user is null)
        {
            return ResendEmailConfirmationStatus.InvalidEmail;
        }

        if (user.Status > UserStatus.Unverified)
        {
            return ResendEmailConfirmationStatus.EmailIsVerified;
        }

        var emailConfirmation = await context.EmailConfirmations.FirstOrDefaultAsync(e =>
            e.UserId == user.UserId && e.Type == EmailConfirmationType.ConfirmEmail);

        if (emailConfirmation is null)
        {
            emailConfirmation = new EmailConfirmationDAL
            {
                UserId = user.UserId,
                Type = EmailConfirmationType.ConfirmEmail
            };
            context.EmailConfirmations.Add(emailConfirmation);
        }
        else
        {
            if (emailConfirmation.Used)
            {
                return ResendEmailConfirmationStatus.EmailIsVerified;
            }

            if ((DateTimeOffset.UtcNow - emailConfirmation.SentAt).TotalMinutes < 1)
            {
                return ResendEmailConfirmationStatus.TooManyAttempts;
            }
        }

        emailConfirmation.Token = GenerateRegistrationToken(user.UserId);
        emailConfirmation.SentAt = DateTimeOffset.UtcNow;
        emailConfirmation.Used = false;

        await context.SaveChangesAsync();

        var appHost = request.IsClient
            ? applicationSettingsService.DmpHostsSettings.Client
            : applicationSettingsService.DmpHostsSettings.Seller;

        await mailService.CreateConfirmEmailRegistrationMail(
            CreateConfirmationMailModel($"{appHost}/registration/confirm-email/{emailConfirmation.Token}"),
            user.Email,
            language);

        return ResendEmailConfirmationStatus.Success;
    }

    public async Task<SendForgotPasswordStatus> SendForgotPassword(ResendEmailConfirmationRequest request, string lng)
    {
        var emailValue = EmailHelper.Normalize(request.Email);
        var language = LocaleHelper.ConvertLocaleToLanguage(lng);
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == emailValue);

        if (user is null || user.Status > UserStatus.Active)
        {
            return SendForgotPasswordStatus.InvalidEmail;
        }

        var timeLimit = DateTimeOffset.UtcNow.AddMinutes(-60);

        var exists = await context.EmailConfirmations.AsNoTracking().AnyAsync(e =>
            e.UserId == user.UserId && e.Type == EmailConfirmationType.ForgotPassword && e.SentAt > timeLimit);

        if (exists)
        {
            return SendForgotPasswordStatus.TooManyAttempts;
        }

        var emailConfirmation = new EmailConfirmationDAL
        {
            UserId = user.UserId,
            Token = GenerateRegistrationToken(user.UserId),
            SentAt = DateTimeOffset.UtcNow,
            Type = EmailConfirmationType.ForgotPassword,
            Used = false
        };

        context.EmailConfirmations.Add(emailConfirmation);
        await context.SaveChangesAsync();

        var appHost = request.IsClient
            ? applicationSettingsService.DmpHostsSettings.Client
            : applicationSettingsService.DmpHostsSettings.Seller;

        await mailService.CreateForgotPasswordMail(
            CreateConfirmationMailModel($"{appHost}/registration/reset-password/{emailConfirmation.Token}"),
            user.Email,
            language);

        return SendForgotPasswordStatus.Success;
    }

    public async Task<ConfirmEmailStatus> ResetPassword(ResetPasswordRequest request)
    {
        var validationRes = jwtTokenService.ValidateRegistrationToken(request.Token, applicationSettingsService.JwtRegistrationSettings);
        var resultStatus = ConvertTokenStatusToConfirmEmailStatus(validationRes.Status);

        if (resultStatus != ConfirmEmailStatus.Success)
        {
            logger.LogInformation("Forgot password token validation failed: {Error}.", validationRes.Error);
            return resultStatus;
        }

        var nameIdentifierClaim = validationRes.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (nameIdentifierClaim is null || !Guid.TryParse(nameIdentifierClaim.Value, out var userId))
        {
            logger.LogInformation("Forgot password token has an invalid NameIdentifier claim.");
            return ConfirmEmailStatus.InvalidToken;
        }

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var emailConfirmation = await context.EmailConfirmations.FirstOrDefaultAsync(ec =>
            ec.UserId == userId && ec.Token == request.Token && ec.Type == EmailConfirmationType.ForgotPassword);

        if (emailConfirmation is null)
        {
            logger.LogInformation("Forgot password token for user {UserId} was not found.", userId);
            return ConfirmEmailStatus.InvalidToken;
        }

        if (emailConfirmation.Used)
        {
            logger.LogInformation("Forgot password token for user {UserId} has already been used.", userId);
            return ConfirmEmailStatus.UsedToken;
        }

        var user = await context.Users.FirstAsync(u => u.UserId == userId);

        var (hash, salt) = PasswordHelper.HashPassword(request.Password);

        user.Password = hash;
        user.Salt = salt;
        context.Update(user);

        emailConfirmation.Used = true;
        context.Update(emailConfirmation);

        await context.SaveChangesAsync();

        return ConfirmEmailStatus.Success;
    }

    private string GenerateRegistrationToken(Guid userId) =>
        jwtTokenService.GenerateRegistrationToken(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            applicationSettingsService.JwtRegistrationSettings);

    private ConfirmEmailRegistration CreateConfirmationMailModel(string url) => new()
    {
        Url = url,
        LogoUrl = LogoUrl,
        Year = DateTime.UtcNow.Year.ToString()
    };

    private static ConfirmEmailStatus ConvertTokenStatusToConfirmEmailStatus(TokenValidationStatus tokenStatus) => tokenStatus switch
    {
        TokenValidationStatus.Success => ConfirmEmailStatus.Success,
        TokenValidationStatus.TokenExpired => ConfirmEmailStatus.ExpiredToken,
        TokenValidationStatus.InvalidSignature
            or TokenValidationStatus.InvalidAudience
            or TokenValidationStatus.InvalidIssuer
            or TokenValidationStatus.TokenNotYetValid
            or TokenValidationStatus.ValidationError => ConfirmEmailStatus.InvalidToken,
        _ => throw new ArgumentOutOfRangeException(nameof(tokenStatus), tokenStatus, null)
    };
}
