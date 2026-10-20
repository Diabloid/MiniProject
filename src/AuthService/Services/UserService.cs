using System.Net;
using AuthService.DTOs;
using AuthService.Repositories;
using Microsoft.Extensions.Logging;
using MiniProject.Shared.Common;

namespace AuthService.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(int userId)
    {
        var user = await _userRepository.GetUserWithRolesAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found", userId);
            return Result<UserDto>.Failure("Користувача не знайдено.", HttpStatusCode.NotFound);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Roles = roles
        };

        return Result<UserDto>.Success(dto);
    }

    public async Task<Result<PagedResult<UserDto>>> GetUsersPagedAsync(int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var pagedUsers = await _userRepository.GetPagedUsersAsync(page, pageSize);

        var dtos = pagedUsers.Items.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            CreatedAt = u.CreatedAt,
            IsActive = u.IsActive,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
        }).ToList();

        var result = new PagedResult<UserDto>(dtos, pagedUsers.TotalCount, page, pageSize);
        return Result<PagedResult<UserDto>>.Success(result);
    }

    public async Task<Result> UpdateUserRolesAsync(int userId, UpdateRolesRequest request)
    {
        _logger.LogInformation("Updating roles for user {UserId}", userId);

        var user = await _userRepository.GetUserWithRolesAsync(userId);
        if (user == null)
        {
            return Result.Failure("Користувача не знайдено.", HttpStatusCode.NotFound);
        }

        var roles = await _userRepository.GetRolesByNamesAsync(request.Roles);
        if (roles.Count != request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            var foundNames = roles.Select(r => r.Name.ToLower()).ToHashSet();
            var missing = request.Roles.Where(r => !foundNames.Contains(r.ToLower())).ToList();
            return Result.Failure($"Наступні ролі не існують: {string.Join(", ", missing)}", HttpStatusCode.BadRequest);
        }

        await _userRepository.UpdateRolesAsync(user, roles);

        _logger.LogInformation("Roles updated for user {UserId}: {Roles}", userId, string.Join(", ", request.Roles));
        return Result.Success(HttpStatusCode.NoContent);
    }
}
