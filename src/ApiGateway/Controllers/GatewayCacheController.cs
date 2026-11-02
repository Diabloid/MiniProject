using ApiGateway.DTOs;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/gateway")]
[Produces("application/json")]
public class GatewayCacheController : ControllerBase
{
    private readonly IGatewayCacheService _cacheService;

    public GatewayCacheController(IGatewayCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    /// <summary>
    /// Отримання метрик кешу шлюзу (влучання, промахи, відсоток ефективності)
    /// </summary>
    [HttpGet("cache-metrics")]
    [ProducesResponseType(typeof(CacheMetricsDto), StatusCodes.Status200OK)]
    public IActionResult GetMetrics()
    {
        return Ok(_cacheService.GetMetrics());
    }

    /// <summary>
    /// Ручна інвалідація кешу спільних даних каталогу (тільки для Admin)
    /// </summary>
    [HttpPost("cache/invalidate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult InvalidateCache()
    {
        _cacheService.InvalidateCatalog();
        return Ok(new { message = "Кеш каталогу успішно очищено." });
    }
}
