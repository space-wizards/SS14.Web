using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Robust.Shared.AuthLib;
using SS14.Web.Data;

namespace SS14.Web
{
    public sealed class SS14UserValidator : UserValidator<SpaceUser>
    {
        public override async Task<IdentityResult> ValidateAsync(UserManager<SpaceUser> manager, SpaceUser user)
        {
            var baseResult = await base.ValidateAsync(manager, user);

            var errors = baseResult.Errors.ToList();

            ValidateUsernameAsync(user, errors);
            if (errors.All(p => p.Code != nameof(Describer.InvalidEmail)))
            {
                // Don't run our email checks if even ASP.NET's simpler checks fail.
                // Otherwise there would be a duplicate "invalid email" message.
                ValidateEmailAsync(user, errors);
            }

            return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
        }

        private void ValidateEmailAsync(SpaceUser user, List<IdentityError> errors)
        {
            var email = user.Email;

            try
            {
                // TODO: .NET 5 has a Try* version of this, switch to that when .NET 5 is available.
                var _ = new MailAddress(email);
            }
            catch (FormatException)
            {
                errors.Add(Describer.InvalidEmail(email));
            }
        }

        private static void ValidateUsernameAsync(SpaceUser user, List<IdentityError> errors)
        {
            if (UsernameHelpers.IsNameValid(user.UserName, out var reason))
            {
                return;
            }

            errors.Add(new IdentityError
            {
                Code = reason.ToString(),

                Description = reason switch
                {
                    // Pretty sure the empty case is already checked by the parent but whatever.
                    UsernameHelpers.UsernameInvalidReason.Empty => "Username cannot be empty.",
                    UsernameHelpers.UsernameInvalidReason.TooLong => "Username is too long.",
                    UsernameHelpers.UsernameInvalidReason.InvalidCharacter => "Username contains invalid character.",
                    _ => "Unknown error."
                }
            });
        }
    }
}