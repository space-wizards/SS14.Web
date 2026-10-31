using System.Collections.Generic;
using System.Threading.Tasks;
using SS14.Auth.Shared.Emails;

namespace SS14.Web.Tests.Utils;


public sealed class RecordingEmailSender : IRawEmailSender
{
    public List<SentEmail> SentEmails { get; } = [];

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        SentEmails.Add(new SentEmail(email, subject, htmlMessage));
        return Task.CompletedTask;
    }
}

public sealed record SentEmail(string Email, string Subject, string HtmlMessage);

