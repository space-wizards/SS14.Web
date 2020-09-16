using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace SS14.Auth.Data
{
    public class SpaceUser : IdentityUser<Guid>
    {
        public DateTimeOffset CreatedTime { get; set; }
        public List<ActiveSession> ActiveSessions { get; set; } = new List<ActiveSession>();
        public List<AuthHash> AuthHashes { get; set; } = new List<AuthHash>();
    }
}