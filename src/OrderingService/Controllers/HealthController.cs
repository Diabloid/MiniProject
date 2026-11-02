using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingService.Clients;
using OrderingService.Data;

namespace OrderingService.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly OrderingDbContext _context;
    private readonly ICatalogClient _catalogClient;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        OrderingDbContext context,
        ICatalogClient catalogClient,
        ILogger<HealthController> logger)
    {
        _context = context;
        _catalogClient = catalogClient;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        try
        {
            var dbConnected = await _context.Database.CanConnectAsync(ct);
            if (!dbConnected)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    service = "OrderingService",
                    database = "Disconnected",
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                status = "Healthy",
                service = "OrderingService",
                database = "Connected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                service = "OrderingService",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
