using System.Security.Claims;
using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniProject.Shared.Common;

namespace AuthService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Неавторизовано",
                Detail = "Ідентифікатор користувача відсутній у токені."
            });
        }

        var result = await _userService.GetCurrentUserAsync(userId);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка отримання профілю",
                Detail = result.ErrorMessage
            });
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _userService.GetUsersPagedAsync(page, pageSize);
        return Ok(result.Value);
    }

    [HttpPut("{id:int}/roles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoles(int id, [FromBody] UpdateRolesRequest request)
    {
        var result = await _userService.UpdateUserRolesAsync(id, request);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка оновлення ролей",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return NoContent();
    }
}
