using System.Text.Json.Serialization;

namespace NovaGames.UI.Services.Api;

public class ClientLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ClientUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public decimal WalletBalance { get; set; }
}

public class ClientAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public ClientUserDto User { get; set; } = null!;
}

public class ClientGameDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
}

public class ClientUserGameStatusDto
{
    public bool HasPurchased { get; set; }
    public int? OrderId { get; set; }
    public string? Status { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? Message { get; set; }
}

public class ClientGameStoreDetailsDto
{
    public ClientGameDto Game { get; set; } = null!;
    public ClientUserGameStatusDto UserStatus { get; set; } = new();
    public DateTime AggregatedAt { get; set; }
}

public class ClientOrderItemDto
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Subtotal => UnitPrice * Quantity;
}

public class ClientOrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? CustomerNotes { get; set; }
    public DateTime OrderDate { get; set; }
    public List<ClientOrderItemDto> Items { get; set; } = new();
}

public class ClientCreateOrderItemRequest
{
    public int GameId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class ClientCreateOrderRequest
{
    public string PaymentMethod { get; set; } = "Card";
    public string? CustomerNotes { get; set; }
    public List<ClientCreateOrderItemRequest> Items { get; set; } = new();
}

public class ClientPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

public class ClientProblemDetails
{
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

public class RateLimitExceededException : Exception
{
    public int RetryAfterSeconds { get; }
    public ClientProblemDetails? ProblemDetails { get; }

    public RateLimitExceededException(int retryAfterSeconds, ClientProblemDetails? problemDetails = null)
        : base($"Перевищено ліміт запитів. Спробуйте через {retryAfterSeconds} с.")
    {
        RetryAfterSeconds = retryAfterSeconds;
        ProblemDetails = problemDetails;
    }
}

public class ApiException : Exception
{
    public int StatusCode { get; }
    public ClientProblemDetails? ProblemDetails { get; }

    public ApiException(int statusCode, string message, ClientProblemDetails? problemDetails = null)
        : base(message)
    {
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
    }
}
