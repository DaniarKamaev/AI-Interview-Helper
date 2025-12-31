using System.Text.Json.Serialization;

namespace InterviewHelperAPI.Service.GigaChat;

public class AnswerEvaluation
{
    public decimal Score { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public List<string> DetectedSkills { get; set; } = new();
    public List<string> ImprovementAreas { get; set; } = new();
    public string NextQuestionHint { get; set; } = string.Empty;
    
    [JsonIgnore]
    public Dictionary<string, decimal> DetailedScores { get; set; } = new();
}

public class InterviewSummary
{
    public decimal FinalScore { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public string OverallFeedback { get; set; } = string.Empty;
    public List<RecommendedTopic> RecommendedTopics { get; set; } = new();
    public string SuggestedLevel { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }
    public string InterviewDuration { get; set; } = string.Empty;
}

public class RecommendedTopic
{
    public string Topic { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public List<string> Resources { get; set; } = new();
}

public class InterviewSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string JobLevel { get; set; } = string.Empty;
    public string Status { get; set; } = "active"; // active, completed, cancelled
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<QuestionAnswerPair> QAPairs { get; set; } = new();
    public InterviewSummary? Summary { get; set; }
}

public class QuestionAnswerPair
{
    public string Question { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public AnswerEvaluation? Evaluation { get; set; }
    public DateTime AskedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
}