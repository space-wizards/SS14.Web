using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;
using SS14.Auth.Shared.Emails;
using SS14.Auth.Shared.Sessions;

namespace SS14.Web.Areas.Admin.Pages.Users;

public class ViewUser : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly IEmailSender _emailSender;
    private readonly SessionManager _sessionManager;
    private readonly PatreonDataManager _patreonDataManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<SpaceRole> _roleManager;
    private readonly AccountLogManager _accountLogManager;

    public SpaceUser SpaceUser { get; set; }

    [TempData] public string StatusMessage { get; set; }

    [BindProperty] public InputModel Input { get; set; }

    public string PatronTier { get; set; }
        
    public class InputModel
    {
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Username")] public string Username { get; set; }

        [Display(Name = "Email Confirmed?")] public bool EmailConfirmed { get; set; }

        [Display(Name = "Is Auth Hub Admin?")] public bool HubAdmin { get; set; }
        [Display(Name = "Is Server Hub Admin?")] public bool ServerHubAdmin { get; set; }
        
        [Display(Name = "2FA enabled?")] public bool TfaEnabled { get; set; }
        
        [Display(Name = "Locked?")]
        public bool AdminLocked { get; set; }

        [Display(Name = "Administrative notes")]
        public string AdminNotes { get; set; }
    }

    public ViewUser(
        SpaceUserManager userManager, 
        IEmailSender emailSender,
        SessionManager sessionManager,
        PatreonDataManager patreonDataManager,
        ApplicationDbContext dbContext,
        RoleManager<SpaceRole> roleManager,
        AccountLogManager accountLogManager)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _sessionManager = sessionManager;
        _patreonDataManager = patreonDataManager;
        _dbContext = dbContext;
        _roleManager = roleManager;
        _accountLogManager = accountLogManager;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        SpaceUser = await _userManager.FindByIdAsync(id.ToString());

        if (SpaceUser == null)
        {
            return NotFound("That user does not exist!");
        }

        await LoadAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid id)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(); 
        
        var actor = await _userManager.GetUserAsync(User);
        SpaceUser = await _userManager.FindByIdAsync(id.ToString());

        // Field becomes null if empty.
        Input.AdminNotes ??= "";
        
        if (SpaceUser == null)
        {
            return NotFound("That user does not exist!");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        if (SpaceUser.Email != Input.Email)
        {
            await _accountLogManager.Log(SpaceUser, new AccountLogEmailChanged(SpaceUser.Email, Input.Email));
            SpaceUser.Email = Input.Email;
        }

        if (SpaceUser.UserName != Input.Username)
        {
            await _accountLogManager.LogNameChanged(SpaceUser, SpaceUser.UserName, Input.Username);
            SpaceUser.UserName = Input.Username;
        }
        
        if (SpaceUser.EmailConfirmed != Input.EmailConfirmed)
        {
            await _accountLogManager.Log(SpaceUser, new AccountLogEmailConfirmedChanged(Input.EmailConfirmed));
            SpaceUser.EmailConfirmed = Input.EmailConfirmed;
        }

        if (SpaceUser.AdminNotes != Input.AdminNotes)
        {
            await _accountLogManager.Log(SpaceUser, new AccountLogAdminNotesChanged(Input.AdminNotes));
            SpaceUser.AdminNotes = Input.AdminNotes;
        }
        
        if (SpaceUser.AdminLocked != Input.AdminLocked)
        {
            await _accountLogManager.Log(SpaceUser, new AccountLogAdminLockedChanged(Input.AdminLocked));
            SpaceUser.AdminLocked = Input.AdminLocked;
        }

        await CheckRole(Input.HubAdmin, AuthConstants.RoleSysAdmin);
        await CheckRole(Input.ServerHubAdmin, AuthConstants.RoleServerHubAdmin);

        // Can't use UpdateAsync() because it validates email which might not work.
        await _userManager.UpdateNormalizedEmailAsync(SpaceUser);
        await _userManager.UpdateNormalizedUserNameAsync(SpaceUser);
        await _dbContext.SaveChangesAsync();

        await tx.CommitAsync();

        StatusMessage = "Changes saved";

        return RedirectToPage(new {id});

        async Task CheckRole(bool set, string roleName)
        {
            if (set != await _userManager.IsInRoleAsync(SpaceUser, roleName))
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                var roleGuid = Guid.Parse(await _roleManager.GetRoleIdAsync(role));
                
                if (set)
                {
                    await _userManager.AddToRoleAsync(SpaceUser, roleName);
                    await _accountLogManager.Log(SpaceUser, new AccountLogAuthRoleAdded(roleGuid));
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(SpaceUser, roleName);
                    await _accountLogManager.Log(SpaceUser, new AccountLogAuthRoleRemoved(roleGuid));
                }

                await _userManager.UpdateSecurityStampAsync(SpaceUser);
            }
        }
    }

    public async Task<IActionResult> OnPostResendConfirmationAsync(Guid id)
    {
        SpaceUser = await _userManager.FindByIdAsync(id.ToString());

        if (SpaceUser == null)
        {
            return NotFound("That user does not exist!");
        }
        
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(SpaceUser);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var confirmLink = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { area = "Identity", userId = id, code = code},
            protocol: Request.Scheme);

        try
        {
            await ModelShared.SendConfirmEmail(_emailSender, SpaceUser.Email, confirmLink);
            StatusMessage = "Email sent";
        }
        catch (Exception e)
        {
            // Looks awful but better than nothing.
            StatusMessage = $"Error while sending email: {e}";
        }

        return RedirectToPage(new {id});
    }

    public async Task<IActionResult> OnPostLogoutAsync(Guid id)
    {
        SpaceUser = await _userManager.FindByIdAsync(id.ToString());

        if (SpaceUser == null)
        {
            return NotFound("That user does not exist!");
        }

        await _sessionManager.InvalidateSessions(SpaceUser);
        await _userManager.UpdateSecurityStampAsync(SpaceUser);

        StatusMessage = "All sessions logged out";
            
        return RedirectToPage(new {id});
    }

    private async Task LoadAsync()
    {
        Input = new InputModel
        {
            Email = SpaceUser.Email,
            EmailConfirmed = SpaceUser.EmailConfirmed,
            Username = SpaceUser.UserName,
            HubAdmin = await _userManager.IsInRoleAsync(SpaceUser, AuthConstants.RoleSysAdmin),
            ServerHubAdmin = await _userManager.IsInRoleAsync(SpaceUser, AuthConstants.RoleServerHubAdmin),
            TfaEnabled = SpaceUser.TwoFactorEnabled,
            AdminLocked = SpaceUser.AdminLocked,
            AdminNotes = SpaceUser.AdminNotes
        };

        PatronTier = await _patreonDataManager.GetPatreonTierAsync(SpaceUser);
    }
}