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

    public static async Task SendAccountExistsEmail(IEmailSender sender, string address, string loginUrl)
    {
        await sender.SendEmailAsync(address, "Registration attempt on your Space Station 14 account",
            "Someone just tried to register a new Space Station 14 account using this email address, " +
            "but an account already exists for it. No new account was created." +
            $"\n<p>If this was you: you already have an account - just <a href='{loginUrl}'>log in</a>, " +
            "or reset your password from the login page if you've forgotten it.</p>" +
            "\n<p>If this wasn't you, you don't need to do anything. Your account is unaffected.</p>" +
            $"\n<p><small>If the link above is not working, go to {HtmlEncoder.Default.Encode(loginUrl)}</small></p>");
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

    public static SpaceUser CreateNewUser(string userName, string email, ISystemClock systemClock)
    {
        return new SpaceUser { UserName = userName, Email = email, CreatedTime = systemClock.UtcNow };
    }
}
