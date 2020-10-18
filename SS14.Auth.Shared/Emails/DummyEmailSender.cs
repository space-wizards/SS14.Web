using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SS14.Auth.Shared.Emails
{
    public class DummyEmailSender : IEmailSender
    {
        public DummyEmailSender(IConfiguration config)
        {

        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Console.WriteLine("Would send email to {0}:\n" +
                              "Subject: {1}\n" +
                              "Body: {2}", email, subject, htmlMessage);
            return Task.CompletedTask;
        }
    }
}