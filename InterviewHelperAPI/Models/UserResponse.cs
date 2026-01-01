using System;
using System.Collections.Generic;

namespace InterviewHelperAPI;

public partial class UserResponse
{
    public int ResponseId { get; set; }

    public int QuestionId { get; set; }

    public int InterviewId { get; set; }

    public string UserAnswer { get; set; } = null!;

    /// <summary>
    /// Время на ответ в секундах
    /// </summary>
    public int? ResponseTimeSeconds { get; set; }

    /// <summary>
    /// Детальный анализ ответа от ИИ в формате JSON
    /// </summary>
    public string AiAnalysis { get; set; } = null!;

    /// <summary>
    /// Текстовый комментарий от ИИ к ответу
    /// </summary>
    public string? AiComment { get; set; }

    /// <summary>
    /// Обнаруженные навыки в ответе
    /// </summary>
    public string? DetectedSkills { get; set; }

    public DateTime? AnsweredAt { get; set; }

    public virtual Interview Interview { get; set; } = null!;

    public virtual InterviewQuestion Question { get; set; } = null!;
}
