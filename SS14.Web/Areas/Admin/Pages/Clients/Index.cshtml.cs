using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Auth.Shared.Data;
using DbClient = IdentityServer4.EntityFramework.Entities.Client;

namespace SS14.Web.Areas.Admin.Pages.Clients
{
    public class Index : PageModel
    {
        private readonly ApplicationDbContext _dbContext;

        public IEnumerable<(DbClient, UserOAuthClient)> Clients { get; set; }

        public Index(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync()
        {
            // This is a left join
            var query = from c in _dbContext.Clients
                join uc in _dbContext.UserOAuthClients.Include(c => c.SpaceUser)
                    on c.Id equals uc.ClientId into grouping
                from uc in grouping.DefaultIfEmpty()
                orderby c.Created
                select new { c, uc };
            
            Clients = (await query.ToListAsync()).Select(c => (c.c, c.uc));
        }

        public async Task<IActionResult> OnPostNewClientAsync()
        {
            var client = new IdentityServer4.EntityFramework.Entities.Client
            {
                ClientId = Guid.NewGuid().ToString(),
            };

            // ReSharper disable once MethodHasAsyncOverload
            _dbContext.Clients.Add(client);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("./Client", new { id = client.Id });
        }
    }
}