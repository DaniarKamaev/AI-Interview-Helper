using System;
using System.Collections.Generic;

namespace InterviewHelperAPI;

public partial class InterviewQuestion
{
    public int QuestionId { get; set; }

    public int InterviewId { get; set; }

    public int? CategoryId { get; set; }

    public string QuestionText { get; set; } = null!;

    public string? QuestionType { get; set; }

    public string? DifficultyLevel { get; set; }

    /// <summary>
    /// Порядковый номер вопроса в собеседовании
    /// </summary>
    public int TurnNumber { get; set; }

    public DateTime? AskedAt { get; set; }

    public virtual QuestionCategory? Category { get; set; }

    public virtual Interview Interview { get; set; } = null!;

    public virtual ICollection<UserResponse> UserResponses { get; set; } = new List<UserResponse>();
}
