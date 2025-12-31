using System.Text;

namespace InterviewHelperAPI.Service.GigaChat;

public class InterviewContext
{
    public int InterviewId { get; set; }
    public int UserId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string JobLevel { get; set; } = "middle";
    public string JobDescription { get; set; } = string.Empty;
    public List<DialogTurn> ConversationHistory { get; set; } = new();
    public List<SkillEvaluation> SkillEvaluations { get; set; } = new();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    private const int MaxHistoryLength = 10;
    
    public void AddMessage(string role, string content)
    {
        ConversationHistory.Add(new DialogTurn
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow
        });
        
        if (ConversationHistory.Count > MaxHistoryLength)
        {
            ConversationHistory = ConversationHistory
                .Skip(ConversationHistory.Count - MaxHistoryLength)
                .ToList();
        }
    }
    
    public string GetFormattedHistory()
    {
        var sb = new StringBuilder();
        foreach (var message in ConversationHistory)
        {
            sb.AppendLine($"{message.Role}: {message.Content}");
        }
        return sb.ToString();
    }
    
    public decimal CalculateAverageScore()
    {
        if (SkillEvaluations.Count == 0) return 0;
        return Math.Round(SkillEvaluations.Average(s => s.Score), 1);
    }
    
    public Dictionary<string, decimal> GetSkillScores()
    {
        return SkillEvaluations
            .GroupBy(s => s.SkillName)
            .ToDictionary(
                g => g.Key,
                g => Math.Round(g.Average(s => s.Score), 1)
            );
    }
    
    public TimeSpan GetDuration()
    {
        var endTime = CompletedAt ?? DateTime.UtcNow;
        return endTime - StartedAt;
    }
    
    public int GetTotalQuestions()
    {
        return ConversationHistory.Count(m => m.Role == "assistant");
    }
    
    public int GetTotalAnswers()
    {
        return ConversationHistory.Count(m => m.Role == "user");
    }
}

public class DialogTurn
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SkillEvaluation
{
    public string SkillName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Evidence { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}