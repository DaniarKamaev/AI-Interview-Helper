using InterviewHelperAPI.Features.Free.Interview.Commands;
using Microsoft.Extensions.Caching.Memory;

namespace InterviewHelperAPI.Service.GigaChat;

public interface IInterviewSessionManager
{
    Task RegisterSessionAsync(InterviewContext context);
    Task<InterviewContext> StartNewSessionAsync(StartInterviewCommand command);
    Task<InterviewContext?> GetSessionAsync(int interviewId);
    Task UpdateSessionAsync(InterviewContext context);
    Task CompleteSessionAsync(int interviewId);
}

public class InterviewSessionManager : IInterviewSessionManager
{
    private readonly IMemoryCache _cache;
    private readonly HelperDbContext _dbContext;
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(2);
    
    public InterviewSessionManager(IMemoryCache cache, HelperDbContext dbContext)
    {
        _cache = cache;
        _dbContext = dbContext;
    }
    

    public Task RegisterSessionAsync(InterviewContext context)
    {
        var cacheKey = GetCacheKey(context.InterviewId);
        _cache.Set(cacheKey, context, _sessionTimeout);
        return Task.CompletedTask;
    }
    
    public async Task<InterviewContext> StartNewSessionAsync(StartInterviewCommand command)
    {
        var interview = new Interview
        {
            UserId = command.UserId,
            JobTitle = command.JobTitle,
            JobDescription = command.JobDescription,
            JobLevel = command.JobLevel,
            Status = "in_progress",
            StartedAt = DateTime.UtcNow,
            QuestionsCount = 0,
            CorrectAnswersCount = 0
        };
        
        await _dbContext.Interviews.AddAsync(interview);
        await _dbContext.SaveChangesAsync();
        
        var context = new InterviewContext
        {
            UserId = command.UserId,
            JobTitle = command.JobTitle,
            JobLevel = command.JobLevel,
            JobDescription = command.JobDescription,
            StartedAt = DateTime.UtcNow
        };
        
        return context;
    }
    
    
    
    public Task<InterviewContext?> GetSessionAsync(int interviewId)
    {
        var cacheKey = GetCacheKey(interviewId);
        if (_cache.TryGetValue(cacheKey, out InterviewContext context))
        {
            _cache.Set(cacheKey, context, _sessionTimeout);
            return Task.FromResult<InterviewContext?>(context);
        }
        
        return Task.FromResult<InterviewContext?>(null);
    }
    
    public Task UpdateSessionAsync(InterviewContext context)
    {
        var cacheKey = GetCacheKey(context.InterviewId);
        _cache.Set(cacheKey, context, _sessionTimeout);
        return Task.CompletedTask;
    }
    
    public async Task CompleteSessionAsync(int interviewId)
    {
        var cacheKey = GetCacheKey(interviewId);
        _cache.Remove(cacheKey);
        
        var interview = await _dbContext.Interviews.FindAsync(interviewId);
        if (interview != null)
        {
            interview.Status = "completed";
            interview.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }
    
    private string GetCacheKey(int interviewId) => $"interview_session_{interviewId}";
}