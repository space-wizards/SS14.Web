using System;
using System.ComponentModel.DataAnnotations;

namespace SS14.Auth.Shared.Data;

public class UserDeletionQueueEntry
{
    [Required] public Guid SpaceUserId { get; set; }
    public DateTime QueuedOn { get; set; }
}
