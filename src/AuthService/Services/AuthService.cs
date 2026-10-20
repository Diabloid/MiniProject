using System.Net;
using AuthService.DTOs;
using AuthService.Entities;
using AuthService.Repositories;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using MiniProject.Shared.Common;

namespace AuthService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Attempting registration for email: {Email}", request.Email);

        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            _logger.LogWarning("Registration failed: email {Email} already exists", request.Email);
            return Result<AuthResponseDto>.Failure("Користувач із зазначеною електронною адресою вже зареєстрований.", HttpStatusCode.Conflict);
        }

        var defaultRole = await _userRepository.GetRoleByNameAsync("User");
        if (defaultRole == null)
        {
            _logger.LogError("Default role 'User' not found in database");
            return Result<AuthResponseDto>.Failure("Помилка конфігурації системи: роль за замовчуванням не знайдена.", HttpStatusCode.InternalServerError);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        user.UserRoles.Add(new UserRole
        {
            Role = defaultRole
        });

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var roles = new List<string> { defaultRole.Name };
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshExpires = _tokenService.GetRefreshTokenExpiration();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = refreshExpires,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        _logger.LogInformation("User {Email} (ID: {UserId}) registered successfully", user.Email, user.Id);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = MapToUserDto(user, roles)
        };

        return Result<AuthResponseDto>.Success(response, HttpStatusCode.Created);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid login credentials for email: {Email}", request.Email);
            return Result<AuthResponseDto>.Failure("Невірний логін або пароль.", HttpStatusCode.Unauthorized);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user {Email} attempted login", request.Email);
            return Result<AuthResponseDto>.Failure("Обліковий запис заблоковано.", HttpStatusCode.Unauthorized);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshExpires = _tokenService.GetRefreshTokenExpiration();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = refreshExpires,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = MapToUserDto(user, roles)
        };

        return Result<AuthResponseDto>.Success(response, HttpStatusCode.OK);
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        _logger.LogInformation("Token refresh requested");

        var existingToken = await _refreshTokenRepository.GetByTokenWithUserAsync(request.RefreshToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            _logger.LogWarning("Token refresh failed: token invalid, expired or revoked");
            return Result<AuthResponseDto>.Failure("Недійсний або відкликаний refresh-токен.", HttpStatusCode.Unauthorized);
        }

        var user = existingToken.User;
        if (!user.IsActive)
        {
            _logger.LogWarning("User {UserId} inactive during refresh attempt", user.Id);
            return Result<AuthResponseDto>.Failure("Користувача заблоковано.", HttpStatusCode.Unauthorized);
        }

        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.ReplacedByToken = newRefreshTokenValue;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshExpires = _tokenService.GetRefreshTokenExpiration();

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = refreshExpires,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        _logger.LogInformation("Token rotated successfully for user ID: {UserId}", user.Id);

        var response = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = MapToUserDto(user, roles)
        };

        return Result<AuthResponseDto>.Success(response, HttpStatusCode.OK);
    }

    public async Task<Result> LogoutAsync(string refreshToken)
    {
        _logger.LogInformation("Logout requested");

        var token = await _refreshTokenRepository.GetByTokenWithUserAsync(refreshToken);
        if (token == null || !token.IsActive)
        {
            return Result.Failure("Токен не знайдено або він вже відкликаний.", HttpStatusCode.Unauthorized);
        }

        token.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync();

        _logger.LogInformation("User {UserId} logged out", token.UserId);
        return Result.Success(HttpStatusCode.NoContent);
    }

    private static UserDto MapToUserDto(User user, IEnumerable<string> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Roles = roles.ToList()
        };
    }
}
