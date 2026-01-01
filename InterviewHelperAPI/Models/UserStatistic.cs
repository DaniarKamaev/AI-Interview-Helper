using System;
using System.Collections.Generic;

namespace InterviewHelperAPI;

public partial class UserStatistic
{
    public int StatId { get; set; }

    public int UserId { get; set; }

    public int? TotalInterviews { get; set; }

    public int? CompletedInterviews { get; set; }

    public decimal? AvgTotalScore { get; set; }

    public decimal? BestScore { get; set; }

    public int? AvgDurationSeconds { get; set; }

    public string? StrongestSkill { get; set; }

    public string? WeakestSkill { get; set; }

    public DateTime? LastInterviewDate { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
