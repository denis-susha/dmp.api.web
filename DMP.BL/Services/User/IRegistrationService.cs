using DMP.BL.Models.Registration;
using DMP.BL.RequestModels;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Services.User;

public interface IRegistrationService
{
    Task<RegistrationResultStatus> RegisterSeller(RegistrationRequest request, string lng, string? remoteIp);
    Task<RegistrationResultStatus> RegisterClient(RegistrationRequest request, string lng, string? remoteIp);
    Task<ConfirmEmailResult> ConfirmEmail(string token);
    Task<ResendEmailConfirmationStatus> ResendEmailConfirmation(ResendEmailConfirmationRequest request, string lng);
    Task<SendForgotPasswordStatus> SendForgotPassword(ResendEmailConfirmationRequest request, string lng);
    Task<ConfirmEmailStatus> ResetPassword(ResetPasswordRequest request);
}
