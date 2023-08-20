using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.AuthLib;
using SS14.Auth.Shared.Data;

namespace SS14.Auth.Shared;

public sealed class SS14UserValidator : UserValidator<SpaceUser>
{
    private readonly ApplicationDbContext _dbContext;

    public SS14UserValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task<IdentityResult> ValidateAsync(UserManager<SpaceUser> manager, SpaceUser user)
    {
        var baseResult = await base.ValidateAsync(manager, user);

        var errors = baseResult.Errors.ToList();

        ValidateUsernameAsync(user, errors);
        if (errors.All(p => p.Code != nameof(Describer.InvalidEmail)))
        {
            // Don't run our email checks if even ASP.NET's simpler checks fail.
            // Otherwise there would be a duplicate "invalid email" message.
            await ValidateEmailAsync(user, errors);
        }

        return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
    }

    private async Task ValidateEmailAsync(SpaceUser user, List<IdentityError> errors)
    {
        var email = user.Email;

        string domain;
        try
        {
            // TODO: .NET 5 has a Try* version of this, switch to that when .NET 5 is available.
            var parsedEmail = new MailAddress(email);
            domain = parsedEmail.Host;
        }
        catch (FormatException)
        {
            errors.Add(Describer.InvalidEmail(email));
            return;
        }

        if (await _dbContext.WhitelistEmails.AnyAsync(p => p.Domain == domain))
            return;

        if (await _dbContext.BurnerEmails.AnyAsync(p => p.Domain == domain))
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
                UsernameHelpers.UsernameInvalidReason.TooShort => $"Username is too short (min {UsernameHelpers.NameLengthMin} chars).",
                UsernameHelpers.UsernameInvalidReason.TooLong => $"Username is too long (max {UsernameHelpers.NameLengthMax} chars).",
                UsernameHelpers.UsernameInvalidReason.InvalidCharacter => "Username contains invalid character.",
                _ => "Unknown error."
            }
        });
    }
}