#nullable enable
using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Admin.Pages.Users;

public class CreateReserved : PageModel
{
    private readonly ISystemClock _systemClock;
    private readonly SpaceUserManager _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly AccountLogManager _accountLogManager;

    [BindProperty] public InputModel Input { get; set; }

    public sealed class InputModel
    {
        [Required] public string Username { get; set; } = "";
    }
    
    public CreateReserved(ISystemClock systemClock, SpaceUserManager userManager, ApplicationDbContext dbContext, AccountLogManager accountLogManager)
    {
        _systemClock = systemClock;
        _userManager = userManager;
        _dbContext = dbContext;
        _accountLogManager = accountLogManager;
    }
    
    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userName = Input.Username.Trim();
        if (!ModelState.IsValid)
            return Page();

        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        
        var user = ModelShared.CreateNewUser(userName, $"reserved+{userName}@spacestation14.com", _systemClock);
        user.AdminLocked = true;
        user.AdminNotes = "Account reserved via admin panel. If unlocking, change email and password!";
        user.EmailConfirmed = true;
        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _accountLogManager.LogAndSave(user, new AccountLogCreatedReserved());

            await tx.CommitAsync();
            
            TempData["StatusMessage"] = "Reserved account created";
            return RedirectToPage("ViewUser", new { id = user.Id });
        }
        
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}