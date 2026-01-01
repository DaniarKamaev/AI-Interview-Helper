using System;
using System.Collections.Generic;

namespace InterviewHelperAPI;

public partial class SkillEvaluation
{
    public int EvaluationId { get; set; }

    public int InterviewId { get; set; }

    public string SkillName { get; set; } = null!;

    /// <summary>
    /// programming, database, soft_skills, etc.
    /// </summary>
    public string SkillCategory { get; set; } = null!;

    public decimal Score { get; set; }

    /// <summary>
    /// Уверенность ИИ в оценке (0-1)
    /// </summary>
    public decimal? ConfidenceScore { get; set; }

    /// <summary>
    /// Примеры из ответов, подтверждающие оценку
    /// </summary>
    public string? Evidence { get; set; }

    /// <summary>
    /// Конкретные рекомендации по улучшению
    /// </summary>
    public string? ImprovementSuggestions { get; set; }

    /// <summary>
    /// Финальная оценка после всего собеседования
    /// </summary>
    public bool? IsFinal { get; set; }

    public DateTime? EvaluatedAt { get; set; }

    public virtual Interview Interview { get; set; } = null!;
}
