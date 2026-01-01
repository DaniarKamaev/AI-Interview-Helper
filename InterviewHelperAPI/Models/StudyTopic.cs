using System;
using System.Collections.Generic;

namespace InterviewHelperAPI;

public partial class StudyTopic
{
    public int TopicId { get; set; }

    public int InterviewId { get; set; }

    public string SkillName { get; set; } = null!;

    public string TopicName { get; set; } = null!;

    public string? Priority { get; set; }

    /// <summary>
    /// Почему эта тема рекомендуется
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Ссылки на материалы в формате JSON
    /// </summary>
    public string? Resources { get; set; }

    public bool? IsCompleted { get; set; }

    public DateTime? AddedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Interview Interview { get; set; } = null!;
}
