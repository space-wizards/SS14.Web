using System.Threading.Tasks;

namespace SS14.Auth.Shared.Emails;

// AspNet's built-in one is marked as "for internal use" so...
public interface IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage);
}