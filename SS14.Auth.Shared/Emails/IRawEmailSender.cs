namespace SS14.Auth.Shared.Emails;

/// <summary>
/// Implementation that actually sends the emails.
/// Does not do error handling or rate limiting or anything. That's for <see cref="EmailSender"/>.
/// </summary>
public interface IRawEmailSender : IEmailSender
{
    
}