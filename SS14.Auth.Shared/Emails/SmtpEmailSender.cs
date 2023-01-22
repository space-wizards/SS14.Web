using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SS14.Auth.Shared.Emails;

public class SmtpEmailSender : IRawEmailSender
{
    private readonly SmtpEmailOptions _options;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port);
        await client.AuthenticateAsync(_options.User, _options.Pass);

        // TODO: Maybe instead use the username associated with the account we're sending an email for?
        var username = email.Split("@")[0];

        var msg = new MimeMessage
        {
            Subject = subject,
            Body = new TextPart("html") {Text = htmlMessage},
            From = {new MailboxAddress(_options.SendName, _options.SendEmail)},
            To = {new MailboxAddress(username, email)}
        };

        await client.SendAsync(msg);
    }
}