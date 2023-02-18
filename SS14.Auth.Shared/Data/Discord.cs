using System;
using System.ComponentModel.DataAnnotations;

namespace SS14.Auth.Shared.Data;

public class Discord
{
    public int Id { get; set; }

    [Required] public string DiscordId { get; set; }
    [Required] public Guid SpaceUserId { get; set; }
    public SpaceUser SpaceUser { get; set; }
}
