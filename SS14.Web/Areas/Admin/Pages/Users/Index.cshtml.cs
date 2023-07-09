using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Auth.Shared.Data;
using SS14.Web.Helpers;

namespace SS14.Web.Areas.Admin.Pages.Users;

public class Index : PageModel
{
    public const int PerPage = 100;

    private readonly UserManager<SpaceUser> _userManager;

    public PaginatedList<SpaceUser> UsersList { get; set; }
    public string NameSort { get; set; }
    public string DateSort { get; set; }
    public string EmailSort { get; set; }
    public string ConfirmedSort { get; set; }
    public string CurrentFilter { get; set; }
    public string CurrentSort { get; set; }


    public Index(UserManager<SpaceUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task OnGetAsync(
        string sortOrder,
        string currentFilter,
        string searchString,
        int? pageIndex)
    {
        CurrentSort = sortOrder;
        NameSort = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        DateSort = sortOrder == "date" ? "date_desc" : "date";
        EmailSort = sortOrder == "email" ? "email_desc" : "email";
        ConfirmedSort = sortOrder == "confirmed" ? "confirmed_desc" : "confirmed";

        if (searchString != null)
        {
            pageIndex = 0;
        }
        else
        {
            searchString = currentFilter;
        }

        CurrentFilter = searchString;

        var userQuery = _userManager.Users;
        if (!string.IsNullOrEmpty(searchString))
        {
            var search = searchString.Trim();
            var normalized = search.ToUpperInvariant();
            userQuery = userQuery.Where(u =>
                u.NormalizedEmail.Contains(normalized) || u.NormalizedUserName.Contains(normalized));
        }
            
        switch (sortOrder)
        {
            case "name_desc":
                userQuery = userQuery.OrderByDescending(s => s.UserName);
                break;
            case "date":
                userQuery = userQuery.OrderBy(s => s.CreatedTime);
                break;
            case "date_desc":
                userQuery = userQuery.OrderByDescending(s => s.CreatedTime);
                break;
            case "email":
                userQuery = userQuery.OrderBy(s => s.Email);
                break;
            case "email_desc":
                userQuery = userQuery.OrderByDescending(s => s.Email);
                break;
            case "confirmed":
                userQuery = userQuery.OrderBy(s => s.EmailConfirmed);
                break;
            case "confirmed_desc":
                userQuery = userQuery.OrderByDescending(s => s.EmailConfirmed);
                break;
            default:
                userQuery = userQuery.OrderBy(s => s.UserName);
                break;
        }
            
        UsersList = await PaginatedList<SpaceUser>.CreateAsync(userQuery, pageIndex ?? 0, PerPage);
    }
}