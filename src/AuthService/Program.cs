using AuthService.Data;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.EntityFrameworkCore;
using MiniProject.Shared.Middleware;
using MiniProject.Shared.OpenApi;
using MiniProject.Shared.Security;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddJwtAuth(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddConfiguredOpenApi();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("NovaGames AuthService API")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
