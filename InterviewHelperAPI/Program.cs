using System.Text;
using InterviewHelperAPI;
using InterviewHelperAPI.Features.Auth.Auth;
using InterviewHelperAPI.Features.Auth.Registration;
using InterviewHelperAPI.Features.Free.Interview.Commands;
using InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse;
using InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse.Db;
using InterviewHelperAPI.Service.GigaChat;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddOpenApi();
builder.Services.Configure<GigaChatSettings>(builder.Configuration.GetSection("GigaChat"));

// Настройка HttpClient для GigaChat с обходом SSL (только для разработки)
builder.Services.AddHttpClient<GigaChatService>("GigaChat")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = 
                (message, cert, chain, sslPolicyErrors) =>
                {
                    if (message.RequestUri?.Host.Contains("sberbank.ru") == true ||
                        message.RequestUri?.Host.Contains("devices.sberbank.ru") == true)
                    {
                        return true;
                    }
                    return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
                };
        }
        
        return handler;
    });

builder.Services.AddScoped<IGigaChatService, GigaChatService>();
builder.Services.AddScoped<IInterviewSessionManager, InterviewSessionManager>();
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddMemoryCache();

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey))
{
    jwtKey = "your-development-key-at-least-32-characters-long-123456";
    Console.WriteLine("WARNING: Using default JWT key for development");
}

if (string.IsNullOrEmpty(jwtIssuer))
{
    jwtIssuer = "InterviewHelperAPI";
}

if (string.IsNullOrEmpty(jwtAudience))
{
    jwtAudience = "InterviewHelperClient";
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    
    if (builder.Environment.IsDevelopment())
    {
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully");
                return Task.CompletedTask;
            }
        };
    }
});

builder.Services.AddDbContext<HelperDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 0))
    ));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication();
app.UseAuthorization();

app.RegistationMap();
app.AuthMap();
app.StartInterviewMap();
app.UserResponseMap();

app.MapGet("/", () => "Interview Helper API is running!");
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/api/test", () => "Test endpoint is working!");

app.Run();