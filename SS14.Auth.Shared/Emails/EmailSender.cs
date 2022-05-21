using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.MutexDb;

namespace SS14.Auth.Shared.Emails;

public sealed class EmailSender : IEmailSender
{
    private readonly IRawEmailSender _rawEmailSender;
    private readonly MutexDatabase _mutex;
    private readonly IOptions<LimitOptions> _limits;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IRawEmailSender rawEmailSender, MutexDatabase mutex, IOptions<LimitOptions> limits, ILogger<EmailSender> logger)
    {
        _rawEmailSender = rawEmailSender;
        _mutex = mutex;
        _limits = limits;
        _logger = logger;
    }
    
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var count = _mutex.IncCount("Email");

        if (count >= _limits.Value.MaxEmailsPerHour)
        {
            _logger.LogCritical("HIT EMAIL LIMIT, no more emails will be sent for the rest of the hour!!!");
            return;
        }
        
        await _rawEmailSender.SendEmailAsync(email, subject, htmlMessage);
    }
}