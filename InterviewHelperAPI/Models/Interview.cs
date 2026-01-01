using System;
using System.Collections.Generic;

namespace InterviewHelperAPI;

public partial class Interview
{
    public int InterviewId { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Название позиции (например, &quot;Python-разработчик&quot;)
    /// </summary>
    public string JobTitle { get; set; } = null!;

    /// <summary>
    /// Описание вакансии от пользователя
    /// </summary>
    public string JobDescription { get; set; } = null!;

    public string? JobLevel { get; set; }

    public string? Status { get; set; }

    /// <summary>
    /// Итоговый балл от 1.00 до 10.00
    /// </summary>
    public decimal? TotalScore { get; set; }

    public int? QuestionsCount { get; set; }

    public int? CorrectAnswersCount { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Длительность собеседования в секундах
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Общий фидбэк от ИИ
    /// </summary>
    public string? AiFeedbackSummary { get; set; }

    /// <summary>
    /// Общие рекомендации по изучению
    /// </summary>
    public string? Recommendations { get; set; }

    public virtual ICollection<InterviewQuestion> InterviewQuestions { get; set; } = new List<InterviewQuestion>();

    public virtual ICollection<SkillEvaluation> SkillEvaluations { get; set; } = new List<SkillEvaluation>();

    public virtual ICollection<StudyTopic> StudyTopics { get; set; } = new List<StudyTopic>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserResponse> UserResponses { get; set; } = new List<UserResponse>();
}
