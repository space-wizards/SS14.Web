using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SS14.ServerHub.Controllers
{
    [ApiController]
    [Route("/api/servers")]
    public class ServerListController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServerListController> _logger;

        public ServerListController(IConfiguration configuration, ILogger<ServerListController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<ServerInfo> Get()
        {
            return (IEnumerable<ServerInfo>?) _configuration.GetSection("Servers").Get<List<ServerInfo>?>() ??
                   Array.Empty<ServerInfo>();
        }

        public sealed class ServerInfo
        {
            // For deserialization from config.
            public ServerInfo()
            {
                Address = default!;
                Name = default!;
            }

            public ServerInfo(string name, string address)
            {
                Address = address;
                Name = name;
            }

            public string Address { get; set; }
            public string Name { get; set; }
        }
    }
}