using MediatR;
using System.Linq;
using InterviewHelperAPI.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace InterviewHelperAPI.Features.Free.Interview.Commands.GetAllInterview;

public class GetHandler : IRequestHandler<GetRequest, GetResponse>
{
    private readonly HelperDbContext _db; 
    private readonly IHttpContextAccessor _contextAccessor;
    
    public GetHandler(HelperDbContext db,  IHttpContextAccessor contextAccessor)
    {
        _db = db;
        _contextAccessor = contextAccessor;
    }
    
    public async Task<GetResponse> Handle(GetRequest request, CancellationToken cancellationToken)
    {
        int userId = AuthHelper.GetCurrentUserId(_contextAccessor);
        
        
        var allInterviews = await _db.Interviews
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
    
        return new GetResponse(allInterviews.Count, allInterviews);
    }
}