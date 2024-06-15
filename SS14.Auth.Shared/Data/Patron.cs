using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SS14.Auth.Shared.Data;

public class Patron
{
    public int Id { get; set; }

    [Required] public string PatreonId { get; set; }
    [Required] public Guid SpaceUserId { get; set; }

    [JsonIgnore]
    public SpaceUser SpaceUser { get; set; }

    public string CurrentTier { get; set; }
}
