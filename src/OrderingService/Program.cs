using Microsoft.EntityFrameworkCore;
using MiniProject.Shared.Middleware;
using MiniProject.Shared.OpenApi;
using MiniProject.Shared.Security;
using OrderingService.Clients;
using OrderingService.Data;
using OrderingService.Repositories;
using OrderingService.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. База даних: для тестів InMemory, для роботи MySQL
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<OrderingDbContext>(options =>
        options.UseInMemoryDatabase("OrderingIntegrationTestsDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<OrderingDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}

// 2. HttpClient до CatalogService з обов'язковим таймаутом (2 сек)
var catalogBaseUrl = builder.Configuration["Services:CatalogUrl"] ?? "http://localhost:5002";
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(client =>
{
    client.BaseAddress = new Uri(catalogBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(2);
});

// 3. DI для репозиторіїв та бізнес-сервісів
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// 4. JWT автентифікація через Shared
builder.Services.AddJwtAuth(builder.Configuration);

// 5. Контролери та OpenAPI
builder.Services.AddControllers();
builder.Services.AddConfiguredOpenApi();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("NovaGames OrderingService API")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
