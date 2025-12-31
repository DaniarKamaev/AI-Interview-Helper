using MediatR;
namespace InterviewHelperAPI.Features.Auth.Registration;

public class RegistationHandler : IRequestHandler<RegistationRequest, RegistationResponse>
{
    private readonly HelperDbContext _db;
    private readonly IConfiguration _config;
    
    public RegistationHandler(HelperDbContext db,  IConfiguration config)
    {
        _db = db;
        _config = config;
    }
    public async Task<RegistationResponse> Handle(RegistationRequest request, CancellationToken token)
    {
        string password = HashCreater.HashPassword(request.Password);
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = password,
            CreatedAt = DateTime.UtcNow
        };
        try
        {
            await _db.Users.AddAsync(user, token);
            await _db.SaveChangesAsync(token);
        
            string jwtToken = AuthHelper.GenerateJwtToken(user, _config);
        
            return new RegistationResponse(jwtToken, true, $"Добро пожаловать {user.Username}");
        }
        catch (Exception e)
        {
            return new RegistationResponse(null, false, $"Ошибка при регистрации {e.Message}");
        }
        
    }
}