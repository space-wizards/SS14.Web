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
            $"\n<p><small>If the above link is not working, try this one {HtmlEncoder.Default.Encode(callbackUrl)}</small></p>");
    }

    /// <summary>
    /// Email support@spacestation14.com a list of the deleted accounts so we can manually delete them from the forum and the game db
    /// Remove this once the gdpr deletion flow is fully automated
    /// </summary>
    /// <param name="emailSender">The email sender instance used for sending</param>
    /// <param name="deletedAccounts">The list of usernames and ids of the deleted accounts</param>
    public static async Task SendUserDeletionQueueEmail(IEmailSender emailSender, List<(Guid, string, string)> deletedAccounts)
    {
        await emailSender.SendEmailAsync(
            "support@spacestation14.com",
            "Processed account deletions",
            $"Deleted {deletedAccounts.Count} accounts:\n{string.Join("\n", deletedAccounts.Select(x => $"{x.Item2} | {x.Item1}"))}");
    }

    public static async Task SendDeletionFailedEmail(IEmailSender emailSender, Guid entrySpaceUserId)
    {
        await emailSender.SendEmailAsync(
            "support@spacestation14.com",
            "Failed to delete account",
            $"Failed to delete account with id {entrySpaceUserId}");
    }

    public static async Task SendDeletionQueuedEmail(IEmailSender emailSender, string email, DateTime deletionDate)
    {
        // i.E. "June 15, 2023"
        var deletionDateString = deletionDate.ToString("MMMM dd, yyyy");

        await emailSender.SendEmailAsync(
            email,
            "Account deletion queued",
            $"We received a request to delete your Space Station 14 account. It will be permanently deleted on {deletionDateString}.<br />If you want to keep your account, you can cancel the deletion at any time by logging in at https://account.spacestation14.com<br />Once the deletion goes through, your account can't be recovered. This includes your login, any OAuth apps you've created and all first party data we keep on you.");
    }

    public static async Task SendDeletionCanceledEmail(IEmailSender emailSender, string email)
    {
        await emailSender.SendEmailAsync(
            email,
            "Account deletion canceled",
            "Your scheduled account deletion has been canceled. Your Space Station 14 account is safe and nothing has been removed.");
    }

    public static async Task SendUserDeletedEmail(IEmailSender emailSender, string email)
    {
        await emailSender.SendEmailAsync(
            email,
            "Account deleted",
            "Your Space Station 14 account has been permanently deleted.<br/>This is the result of a deletion request<br/>OR<br>/Your account was deleted by account support.<br/>Your login and associated data has been removed, and you will no longer be able to sign in or play on servers using this account. This can't be undone, but you're welcome to create a new account at any time.");
    }

    public static SpaceUser CreateNewUser(string userName, string email, ISystemClock systemClock)
    {
        return new SpaceUser {UserName = userName, Email = email, CreatedTime = systemClock.UtcNow};
    }
}
