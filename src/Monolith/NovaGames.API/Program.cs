using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using NovaGames.API.Data;
using NovaGames.API.Services;
using NovaGames.Core.Entities;
using NovaGames.Core.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<NovaGames.Core.Interfaces.ILogger, JsonLogger>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NovaGames API", Version = "v1", Description = "API для магазину та лаунчера NovaGames" });

    // Додаємо кнопку "Authorize" в Swagger UI для JWT токенів
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Введіть 'Bearer' [пробіл] і ваш токен.\r\n\r\nПриклад: \"Bearer eyJhbGci...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] { }
                    }
                });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Реєструємо AppDbContext у DI-контейнері
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Налаштування JWT аутентифікації
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,             // Перевіряти, хто видав токен
        ValidateAudience = true,           // Перевіряти, для кого він виданий
        ValidateLifetime = true,           // Перевіряти термін дії (Expired)
        ValidateIssuerSigningKey = true,   // Перевіряти криптографічний підпис

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

var app = builder.Build();

// Забезпечуємо, що при запуску додатку в базі даних буде створений SuperAdmin, якщо його ще немає
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Шукаємо, чи є в базі хоч один SuperAdmin
    if (!dbContext.Users.Any(u => u.Role == UserRole.SuperAdmin))
    {
        var superAdmin = new User
        {
            Username = "cute_metalhead",
            Role = UserRole.SuperAdmin,
            WalletBalance = 10000,
            LastLoginDate = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("NovaStore2026")
        };

        dbContext.Users.Add(superAdmin);
        dbContext.SaveChanges();

        Console.WriteLine("SuperAdmin успішно створений у базі даних.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

app.UseMiddleware<NovaGames.API.Middleware.ExceptionMiddleware>();

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();