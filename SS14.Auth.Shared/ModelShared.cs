using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Email support@spacestation14.com a list of the deleted accounts so we can manually delete them from the forum and the game db
    /// Remove this once the gdpr deletion flow is fully automated
    /// </summary>
    /// <param name="emailSender">The email sender instance used for sending</param>
    /// <param name="deletedAccounts">The list of usernames and ids of the deleted accounts</param>
    public static async Task SendUserDeletionQueueEmail(IEmailSender emailSender, List<(Guid, string)> deletedAccounts)
    {
        await emailSender.SendEmailAsync(
            "support@spacestation14.com",
            "Processed account deletions",
            $"Deleted {deletedAccounts.Count} accounts:\n{string.Join("\n", deletedAccounts.Select(x => $"{x.Item2} | {x.Item1}"))}");
    }

    public static SpaceUser CreateNewUser(string userName, string email, ISystemClock systemClock)
    {
        return new SpaceUser {UserName = userName, Email = email, CreatedTime = systemClock.UtcNow};
    }
}
