using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace SS14.Web.Data
{
    public class SpaceUser : IdentityUser<Guid>
    {
        public DateTimeOffset CreatedTime { get; set; }
        public List<LoginSession> LoginSessions { get; set; } = new List<LoginSession>();
        public List<AuthHash> AuthHashes { get; set; } = new List<AuthHash>();
    }
}