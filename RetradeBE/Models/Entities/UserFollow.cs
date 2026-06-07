using System;
using System.Collections.Generic;

namespace RetradeBE.Models;

public partial class UserFollow
{
    public string FollowId { get; set; } = null!;

    public string? FollowerId { get; set; }

    public string? FollowedUserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? FollowedUser { get; set; }

    public virtual User? Follower { get; set; }
}
