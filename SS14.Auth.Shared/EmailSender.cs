using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace SS14.Auth.Shared
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            Console.WriteLine(subject);
            Console.WriteLine(message);
            return Task.CompletedTask;
        }
    }
}