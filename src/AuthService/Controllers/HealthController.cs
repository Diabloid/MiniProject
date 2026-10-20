using AuthService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AuthDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            if (canConnect)
            {
                return Ok(new
                {
                    status = "Healthy",
                    service = "AuthService",
                    database = "Connected",
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogWarning("HealthCheck: база даних недоступна");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                service = "AuthService",
                database = "Disconnected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HealthCheck error");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                service = "AuthService",
                database = "Error",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
