using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiGateway.Clients;
using ApiGateway.DTOs;
using ApiGateway.Exceptions;
using ApiGateway.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.IntegrationTests;

public class FakeCatalogServiceClient : ICatalogServiceClient
{
    private readonly IGatewayCacheService _cacheService;
    public int DownstreamCallCount;
    public bool ShouldThrow;
    public CatalogGameDto? CustomGame = new()
    {
        Id = 1,
        Title = "S.T.A.L.K.E.R. 2: Heart of Chornobyl",
        Description = "Next-gen post-apocalyptic shooter",
        Price = 1499.00m,
        FinalPrice = 1499.00m,
        CategoryId = 1,
        CategoryName = "Action / RPG",
        IsActive = true
    };

    public FakeCatalogServiceClient(IGatewayCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<CatalogGameDto?> GetGameByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = $"catalog:item:{id}";
        return await _cacheService.GetOrCreateAsync(cacheKey, TimeSpan.FromSeconds(60), () =>
        {
            Interlocked.Increment(ref DownstreamCallCount);
            if (ShouldThrow)
            {
                throw new ServiceUnavailableException("Каталог недоступний", "Сервіс каталогу не відповідає або перевищено таймаут (2с).");
            }

            if (CustomGame != null && CustomGame.Id == id)
            {
                return Task.FromResult<CatalogGameDto?>(CustomGame);
            }

            return Task.FromResult<CatalogGameDto?>(null);
        });
    }

    public Task<string?> GetCatalogItemsRawAsync(string queryString, CancellationToken ct = default)
    {
        Interlocked.Increment(ref DownstreamCallCount);
        return Task.FromResult<string?>("[{\"id\": 1, \"title\": \"S.T.A.L.K.E.R. 2\"}]");
    }
}

public class FakeOrderingServiceClient : IOrderingServiceClient
{
    public int CallCount;
    public bool ShouldThrow;
    public Dictionary<int, UserGameStatusDto> UserStatuses = new();

    public async Task<UserGameStatusDto?> GetUserGameStatusAsync(int userId, int gameId, string? bearerToken, CancellationToken ct = default)
    {
        await Task.Yield();
        Interlocked.Increment(ref CallCount);
        if (ShouldThrow)
        {
            throw new ServiceUnavailableException("Сервіс замовлень недоступний", "Сервіс замовлень тимчасово недоступний.");
        }

        if (UserStatuses.TryGetValue(userId, out var status))
        {
            return status;
        }

        return new UserGameStatusDto
        {
            HasPurchased = false,
            OrderId = null,
            Status = null,
            PurchaseDate = null,
            IsAvailable = true
        };
    }
}

public class GatewayApiFactory : WebApplicationFactory<Program>
{
    public FakeCatalogServiceClient? FakeCatalog { get; private set; }
    public FakeOrderingServiceClient FakeOrdering { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICatalogServiceClient>();
            services.RemoveAll<IOrderingServiceClient>();

            services.AddSingleton<ICatalogServiceClient>(sp =>
            {
                var cacheService = sp.GetRequiredService<IGatewayCacheService>();
                FakeCatalog = new FakeCatalogServiceClient(cacheService);
                return FakeCatalog;
            });
            services.AddSingleton<IOrderingServiceClient>(FakeOrdering);
        });
    }

    public static string GenerateJwtToken(int userId, string email, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("NovaGames_Microservices_SuperSecretKey_2026_Lab1_Authentication!#$");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "NovaGames.AuthService",
            Audience = "NovaGames.Clients",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
