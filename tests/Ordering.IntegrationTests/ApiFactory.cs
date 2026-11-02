using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrderingService.Clients;
using OrderingService.Data;

namespace Ordering.IntegrationTests;

public class FakeCatalogClient : ICatalogClient
{
    public Task<CatalogItemResponse?> GetItemByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id == 99999)
        {
            return Task.FromResult<CatalogItemResponse?>(null);
        }

        if (id == 503)
        {
            throw new CatalogServiceException("Сервіс каталогу тимчасово недоступний.", System.Net.HttpStatusCode.ServiceUnavailable);
        }

        return Task.FromResult<CatalogItemResponse?>(new CatalogItemResponse
        {
            Id = id,
            Title = id == 1 ? "Cyberpunk 2077: Phantom Liberty" : $"Test Game {id}",
            Price = 1199.00m,
            Discount = 15.00m,
            FinalPrice = 1019.15m,
            IsActive = true
        });
    }
}

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Підміняємо клієнт каталогу фейком
            var catalogDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICatalogClient));
            if (catalogDescriptor != null)
            {
                services.Remove(catalogDescriptor);
            }
            services.AddScoped<ICatalogClient, FakeCatalogClient>();

            // Забезпечуємо початковий стан тестової бази
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public string GenerateJwtToken(int userId = 1, string role = "User")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("NovaGames_Microservices_SuperSecretKey_2026_Lab1_Authentication!#$"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, $"user{userId}@test.com"),
            new(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: "NovaGames.AuthService",
            audience: "NovaGames.Clients",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
