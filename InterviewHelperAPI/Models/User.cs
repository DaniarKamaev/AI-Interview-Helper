using System;
using System.Collections.Generic;

namespace InterviewHelperAPI;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? SubscriptionTier { get; set; }

    public DateTime? SubscriptionExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Interview> Interviews { get; set; } = new List<Interview>();

    public virtual UserStatistic? UserStatistic { get; set; }
}
