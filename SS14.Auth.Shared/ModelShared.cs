using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Internal;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Shared
{
    public static class ModelShared
    {
        public static async Task SendConfirmEmail(IEmailSender sender, string address, string confirmLink)
        {
            await sender.SendEmailAsync(address, "Confirm your Space Station 14 account",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(confirmLink)}'>clicking here</a>.");
        }

        public static async Task SendResetEmail(IEmailSender emailSender, string email, string callbackUrl)
        {
            await emailSender.SendEmailAsync(
                email, "Reset Password",
                "A password reset has been requested for your account.<br />" +
                $"If you did indeed request this, <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>click here</a> to reset your password.<br />" +
                "If you did not request this, simply ignore this email.");
        }

        public static SpaceUser CreateNewUser(string userName, string email, ISystemClock systemClock)
        {
            return new SpaceUser {UserName = userName, Email = email, CreatedTime = systemClock.UtcNow};
        }

        public static async Task<string> GenerateEmailConfirmLink(
            UserManager<SpaceUser> userMgr, IUrlHelper url, HttpRequest request,
            SpaceUser user, string returnUrl = null, bool launcher = false)
        {
            var code = await userMgr.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    userId = user.Id,
                    code = code,
                    returnUrl = returnUrl,
                    launcher = launcher
                },
                protocol: request.Scheme);
            return callbackUrl;
        }
    }
}