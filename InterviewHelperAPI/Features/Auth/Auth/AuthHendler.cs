using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InterviewHelperAPI.Features.Auth.Auth;

public class AuthHendler : IRequestHandler<AuthRequest, AuthResponse>
{
    private readonly HelperDbContext _db;
    private readonly IConfiguration _config;
    public AuthHendler(HelperDbContext db,  IConfiguration config)
    {
        _db = db;
        _config = config;
    }
    
    public async Task<AuthResponse> Handle(AuthRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        bool isValid = HashCreater.VerifyPassword(request.Password, user.PasswordHash);

        if (user == null)
            return new AuthResponse(null, false, "Неверный Логин или Пароль");
        
        if (isValid)
        {
            string token = AuthHelper.GenerateJwtToken(user,  _config);
            return new AuthResponse(token, true, $"Добро пожаловать {user.Username}");
        } else
            return new AuthResponse(null, false, "Неверный Логин или Пароль"); 
    }
}