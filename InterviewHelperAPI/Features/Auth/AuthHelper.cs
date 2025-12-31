using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InterviewHelperAPI.Features.Auth
{
    public static class AuthHelper
    {
        public static string GenerateJwtToken(User user, IConfiguration _configuration)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Email),
                new Claim("UserId", user.UserId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public static int? GetCurrentInterviewId(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor.HttpContext;
    
            if (httpContext?.Request.RouteValues.TryGetValue("interviewId", out var routeValue) == true)
            {
                if (int.TryParse(routeValue?.ToString(), out int id))
                    return id;
            }
    
            if (httpContext?.Request.Query.TryGetValue("interviewId", out var queryValue) == true)
            {
                if (int.TryParse(queryValue.FirstOrDefault(), out int id))
                    return id;
            }
    
            var user = httpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var interviewIdClaim = user.FindFirst("interview_id")?.Value
                                       ?? user.FindFirst("InterviewId")?.Value;
        
                if (int.TryParse(interviewIdClaim, out int id))
                    return id;
            }
    
            return null;
        }
        public static string GetCurrentRole(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return string.Empty;

            var role = user.FindFirst(ClaimTypes.Role)?.Value
                    ?? user.FindFirst("role")?.Value
                    ?? user.FindFirst("Role")?.Value
                    ?? user.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

            return role ?? string.Empty;
        }
        public static int GetCurrentUserId(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return 0; ;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? user.FindFirst("Id")?.Value;

            return int.TryParse(userIdClaim, out int userId) ? userId :
                0;
        }
        public static Guid GetCurrentAuthor_id(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return Guid.Parse("00000000-0000-0000-0000-000000000001"); ;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? user.FindFirst("author_id")?.Value;

            return Guid.TryParse(userIdClaim, out Guid userId) ? userId :
                Guid.Parse("00000000-0000-0000-0000-000000000001");
        }
    }
}

