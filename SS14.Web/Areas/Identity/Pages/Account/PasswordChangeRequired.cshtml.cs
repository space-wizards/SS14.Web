using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SS14.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ForcedPassChangeModel : PageModel
{
    public void OnGet()
    {

    }
}
