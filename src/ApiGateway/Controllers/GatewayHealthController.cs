using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("health")]
public class GatewayHealthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GatewayHealthController> _logger;

    public GatewayHealthController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GatewayHealthController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var servicesToCheck = new Dictionary<string, string>
        {
            ["AuthService"] = _configuration["Services:Auth"] + "/health",
            ["CatalogService"] = _configuration["Services:Catalog"] + "/health",
            ["OrderingService"] = _configuration["Services:Ordering"] + "/health",
            ["Monolith"] = _configuration["Services:Monolith"] + "/health"
        };

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(2);

        var statuses = new Dictionary<string, string>();
        var allHealthy = true;

        foreach (var (serviceName, url) in servicesToCheck)
        {
            try
            {
                var response = await client.GetAsync(url, ct);
                if (response.IsSuccessStatusCode)
                {
                    statuses[serviceName] = "Healthy";
                }
                else
                {
                    statuses[serviceName] = $"Degraded (HTTP {(int)response.StatusCode})";
                    allHealthy = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check failed for {Service}", serviceName);
                statuses[serviceName] = "Unreachable";
                allHealthy = false;
            }
        }

        var result = new
        {
            status = allHealthy ? "Healthy" : "Degraded",
            timestamp = DateTime.UtcNow,
            gateway = "Healthy",
            services = statuses
        };

        return allHealthy ? Ok(result) : StatusCode(StatusCodes.Status200OK, result);
    }
}
