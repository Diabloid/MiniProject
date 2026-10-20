using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка реєстрації",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка автентифікації",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка оновлення токена",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка виходу із системи",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return NoContent();
    }
}
