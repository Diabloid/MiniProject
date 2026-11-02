using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniProject.Shared.Common;
using OrderingService.DTOs;
using OrderingService.Services;

namespace OrderingService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryParameters parameters, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _orderService.GetUserOrdersPagedAsync(userId, parameters, ct);
        return Ok(result.Value);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");

        var result = await _orderService.GetOrderByIdAsync(id, userId, isAdmin, ct);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Замовлення не знайдено",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();

        var result = await _orderService.CreateOrderAsync(userId, request, idempotencyKey, ct);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка оформлення замовлення",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        if (result.StatusCode == System.Net.HttpStatusCode.OK)
        {
            return Ok(result.Value);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _orderService.UpdateOrderAsync(id, userId, request, ct);

        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка оновлення замовлення",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");

        var result = await _orderService.CancelOrderAsync(id, userId, isAdmin, ct);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка скасування замовлення",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(new { id, status = "Cancelled", message = "Замовлення успішно скасовано." });
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderQueryParameters parameters, CancellationToken ct)
    {
        var result = await _orderService.GetAllOrdersPagedAsync(parameters, ct);
        return Ok(result.Value);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
