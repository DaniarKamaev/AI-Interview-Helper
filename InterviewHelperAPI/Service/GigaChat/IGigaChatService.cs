namespace InterviewHelperAPI.Service.GigaChat;

public interface IGigaChatService
{
    Task<string> GenerateQuestionAsync(
        string jobDescription, 
        string jobTitle, 
        string jobLevel, 
        InterviewContext? context);
    
    Task<AnswerEvaluation> EvaluateAnswerAsync(
        string question, 
        string userAnswer, 
        InterviewContext context);
    
    Task<string> GenerateQuestionFromHintAsync(
        string jobDescription, 
        string jobTitle, 
        string jobLevel,
        string hint,
        InterviewContext? context);
    
    Task<InterviewSummary> GenerateSummaryAsync(InterviewContext context);
    
    Task<string> GenerateHintAsync(string question, InterviewContext context);
    
    Task<string> GenerateFeedbackAsync(
        string question, 
        string userAnswer, 
        InterviewContext context);
}