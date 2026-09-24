using System.Text.Json;
using DMP.BL.Constants;
using DMP.BL.Models.Admin;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Mail;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

/// <summary>
/// Queues templated mails in the database; the mail worker renders and sends them.
/// </summary>
public class MailService(IDbContextFactory<DmpDbContext> dmpContextFactory) : IMailService
{
    public Task CreateConfirmEmailRegistrationMail(ConfirmEmailRegistration model, string to, Language language) =>
        CreateTemplatedMail("ConfirmEmailRegistration", model, to, language);

    public Task CreateRegistrationMail(MailRegistration model, string to, Language language) =>
        CreateTemplatedMail("Registration", model, to, language);

    public Task CreateForgotPasswordMail(ConfirmEmailRegistration model, string to, Language language) =>
        CreateTemplatedMail("ForgotPassword", model, to, language);

    public Task CreateFinishModerationMail(FinishModeration model, string to, Language language, FinishModerationStatus status)
    {
        var templateName = status switch
        {
            FinishModerationStatus.NeedsImprovement => "FinishModerationNeedImprovement",
            // The misspelling matches the template name stored in the database.
            FinishModerationStatus.Ready => "FinishModerationReedy",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        return CreateTemplatedMail(templateName, model, to, language);
    }

    private async Task CreateTemplatedMail<TModel>(string templateName, TModel model, string to, Language language)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var templateId = await context.EmailTemplates
            .Where(et => et.Name == templateName && et.Language == language)
            .Select(et => et.EmailTemplateId)
            .FirstAsync();

        context.Mails.Add(new MailDAL
        {
            To = to,
            From = BlConstants.DmpEmails["noreply"],
            Status = MailStatus.New,
            EmailTemplateId = templateId,
            Model = JsonSerializer.Serialize(model)
        });
        await context.SaveChangesAsync();
    }
}
