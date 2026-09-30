using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;

namespace SS14.Auth.Shared;

public static class ModelShared
{
    public static async Task SendConfirmEmail(IEmailSender sender, string address, string confirmLink)
    {
        await sender.SendEmailAsync(address, "Confirm your Space Station 14 account",
            $"Please confirm your account by <a href='{confirmLink}'>clicking here</a>." +
            $"\n<p><small>If the above link is not working, try this one {HtmlEncoder.Default.Encode(confirmLink)}</small></p>");
    }

    public static async Task SendResetEmail(IEmailSender emailSender, string email, string callbackUrl)
    {
        await emailSender.SendEmailAsync(
            email, "Reset Password",
            "A password reset has been requested for your account.<br />" +
            $"If you did indeed request this, <a href='{callbackUrl}'>click here</a> to reset your password.<br />" +
            "If you did not request this, simply ignore this email." +
            $"\n<p><small>If the above link is not working, try this one {HtmlEncoder.Default.Encode(callbackUrl)}</small></p");
    }

    public static async Task Send2FaDisabledEmail(IEmailSender emailSender, string email)
    {
        await emailSender.SendEmailAsync(email,
            "Your Space Station 14 account 2fa was disabled",
            $"This email was sent to you to confirm that 2fa has been disabled on your account. If this was you feel free to ignore this email." +
            $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");
    }

    public static async Task Send2FaEnabledEmail(IEmailSender emailSender, string email)
    {
        await emailSender.SendEmailAsync(email,
            "Your Space Station 14 account 2fa was enabled",
            $"This email was sent to you to confirm that 2fa has been enabled on your account. If this was you feel free to ignore this email." +
            $"(And make sure you wrote down your recovery codes)" +
            $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");
    }

    public static async Task Send2FaCodesRegeneratedEmail(IEmailSender emailSender, string email)
    {
        await emailSender.SendEmailAsync(email,
            "Your Space Station 14 account 2fa recovery codes were regenerated",
            $"This email was sent to you to confirm that 2fa recovery codes have been regenerated on your account. If this was you feel free to ignore this email." +
            $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");
    }

    public static async Task Send2FaResetEmail(IEmailSender emailSender, string email)
    {
        await emailSender.SendEmailAsync(email,
            "Your Space Station 14 account 2fa was reset",
            $"This email was sent to you to confirm that 2fa has been reset on your account. If this was you feel free to ignore this email." +
            $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");
    }

    public static async Task SendPersonalDataEmail(IEmailSender emailSender, string email)
    {
        await emailSender.SendEmailAsync(email,
            "Your Space Station 14 account data was requested",
            $"This email was sent to you to confirm your account data was requested. If this was you feel free to ignore this email." +
            $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");
    }

    public static async Task SendUsernameChangeEmail(IEmailSender emailSender, string email, string oldName, string newname)
    {
        await emailSender.SendEmailAsync(email,
            "Your Space Station 14 account username was changed",
            $"This email was sent to you to confirm your username change, you were known as {oldName} but from now on will be known as {newname}. " +
            $"If this was you feel free to ignore this email." +
            $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");
    }

    public static async Task SendPasswordChangedEmail(IEmailSender emailSender, string email)
    {
        await emailSender.SendEmailAsync(email,
            "Your Space Station 14 account password was changed",
            $"This email was sent to you to confirm your password has successfully been changed. If this was you feel free to ignore this email." +
            $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");
    }

    public static async Task SendEmailChangedEmail(IEmailSender emailSender, string email, string oldEmail)
    {
        await emailSender.SendEmailAsync(
            oldEmail,
            "Your Space Station 14 account email was changed",
            $"This email was sent to the old email address for security, if this was you feel free to ignore this email." +
            $"\n\nFurther emails from this point forward will go to {email}." +
            $"\n\nIf this was not you, send an email to support@spacestation14.com immediately.");
    }

    public static SpaceUser CreateNewUser(string userName, string email, ISystemClock systemClock)
    {
        return new SpaceUser {UserName = userName, Email = email, CreatedTime = systemClock.UtcNow};
    }
}
