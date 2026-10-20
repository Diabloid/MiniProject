using CatalogService.DTOs;
using CatalogService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniProject.Shared.Common;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("items")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<GameItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetItems([FromQuery] CatalogQueryParameters parameters)
    {
        var result = await _catalogService.GetItemsPagedAsync(parameters);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка запиту каталогу",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(result.Value);
    }

    [HttpGet("items/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GameItemDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItem(int id)
    {
        var result = await _catalogService.GetItemByIdAsync(id);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Позицію не знайдено",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(result.Value);
    }

    [HttpPost("items")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GameItemDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateItem([FromBody] CreateGameItemRequest request)
    {
        var result = await _catalogService.CreateItemAsync(request);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка створення позиції",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return CreatedAtAction(nameof(GetItem), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("items/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateGameItemRequest request)
    {
        var result = await _catalogService.UpdateItemAsync(id, request);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка оновлення позиції",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return NoContent();
    }

    [HttpDelete("items/{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var result = await _catalogService.DeleteItemAsync(id);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, new ProblemDetails
            {
                Status = (int)result.StatusCode,
                Title = "Помилка видалення позиції",
                Detail = result.ErrorMessage,
                Instance = HttpContext.Request.Path
            });
        }

        return NoContent();
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _catalogService.GetCategoriesAsync();
        return Ok(result.Value);
    }
}
