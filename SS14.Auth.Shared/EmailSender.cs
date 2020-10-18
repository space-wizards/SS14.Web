using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;

namespace SS14.Auth.Shared
{
    public class EmailSender : IEmailSender
    { 
        private SmtpClient smtpClient;
        public EmailSender()
        {
            string cfgfile = Path.Combine(Environment.CurrentDirectory,"smtp.conf");
            
            using (StreamReader smtpconf = new StreamReader(File.Open(cfgfile,FileMode.Open)))
            {
                var smtpServer = smtpconf.ReadLine();
                var smtpUser = smtpconf.ReadLine();
                var smtpPass = smtpconf.ReadLine();               

                smtpClient = new SmtpClient();
                smtpClient.Connect(smtpServer,587);
                smtpClient.Authenticate(smtpUser,smtpPass);

            }    
        }
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var username = email.Split("@")[0];
            
            MimeMessage msg = new MimeMessage ();
            msg.From.Add (new MailboxAddress ("noreply", "noreply@spacestation14.io"));
            msg.To.Add (new MailboxAddress (username, email));
            msg.Subject = subject;

            msg.Body = new TextPart ("plain") {
                Text = message
            };

            smtpClient.SendAsync(msg);
            return Task.CompletedTask;
        }

    }
}