using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public partial class IndexModel : PageModel
{
    private readonly SpaceUserManager _userManager;
    private readonly SignInManager<SpaceUser> _signInManager;
    private readonly IOptions<AccountOptions> _options;
    private readonly ApplicationDbContext _dbContext;

    public bool CanEditUsername { get; set; }
    public int UsernameChangeDelay => _options.Value.UsernameChangeDays;
    public DateTime NextUsernameChangeAllowed { get; set; }

    public IndexModel(
        SpaceUserManager userManager,
        SignInManager<SpaceUser> signInManager, 
        IOptions<AccountOptions> options,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _options = options;
        _dbContext = dbContext;
    }

    [BindProperty] public string Username { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    private async Task LoadAsync(SpaceUser user)
    {
        var userName = await _userManager.GetUserNameAsync(user);

        Username = userName;
        UpdateCanEditUsername(user);
    }

    private void UpdateCanEditUsername(SpaceUser user)
    {
        if (user.LastUsernameChange is { } change)
        {
            var span = TimeSpan.FromDays(UsernameChangeDelay);
            var now = DateTime.UtcNow;

            CanEditUsername = change + span < now;
            NextUsernameChangeAllowed = change + span;
        }
        else
        {
            CanEditUsername = true;
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostUsernameAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }
        
        Username = Username.Trim();
        if (Username == user.UserName)
        {
            return RedirectToPage();
        }
        
        UpdateCanEditUsername(user);
        if (!CanEditUsername)
        {
            await LoadAsync(user);
            return Page();
        }

        var oldName = user.UserName;

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        var result = await _userManager.SetUserNameAsync(user, Username);
        
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(user);
            return Page();
        }
        
        user.LastUsernameChange = DateTime.UtcNow;
        
        _userManager.LogNameChanged(user, oldName, user.UserName, user);
        
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your username has been changed";
        
        await _dbContext.SaveChangesAsync();
        await tx.CommitAsync();

        return RedirectToPage();
    }
}