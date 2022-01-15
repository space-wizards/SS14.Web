using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;

namespace SS14.Web.Areas.Identity.Pages.Account.Manage;

public class Developer : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<SpaceUser> _userManager;

    public List<UserOAuthClient> OAuthClients { get; set; }

    public Developer(ApplicationDbContext dbContext, UserManager<SpaceUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        OAuthClients = await _dbContext.UserOAuthClients
            .Include(oa => oa.Client)
            .Where(oa => oa.SpaceUser == user)
            .ToListAsync();
    }
}