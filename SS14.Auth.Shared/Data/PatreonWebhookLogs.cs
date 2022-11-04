using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SS14.Auth.Shared.Data;

public class PatreonWebhookLog
{
    public int Id { get; set; }
    public string Trigger { get; set; }
    public DateTimeOffset Time { get; set; }

    [Column(TypeName = "jsonb")] public string Content { get; set; }
}