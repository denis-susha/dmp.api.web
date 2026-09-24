using DMP.BL.Models.Admin;
using DMP.BL.Models.Mail;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Services;

public interface IMailService
{
    Task CreateConfirmEmailRegistrationMail(ConfirmEmailRegistration model, string to, Language language);
    Task CreateRegistrationMail(MailRegistration model, string to, Language language);
    Task CreateForgotPasswordMail(ConfirmEmailRegistration model, string to, Language language);
    Task CreateFinishModerationMail(FinishModeration model, string to, Language language, FinishModerationStatus status);
}
