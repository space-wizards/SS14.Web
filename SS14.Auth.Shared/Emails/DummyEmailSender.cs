using System;
using System.Threading.Tasks;

namespace SS14.Auth.Shared.Emails
{
    public class DummyEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Console.WriteLine("Would send email to {0}:\n" +
                              "Subject: {1}\n" +
                              "Body: {2}", email, subject, htmlMessage);
            return Task.CompletedTask;
        }
    }
}