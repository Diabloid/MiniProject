using AuthService.DTOs;
using MiniProject.Shared.Common;

namespace AuthService.Services;

public interface IUserService
{
    Task<Result<UserDto>> GetCurrentUserAsync(int userId);
    Task<Result<PagedResult<UserDto>>> GetUsersPagedAsync(int page, int pageSize);
    Task<Result> UpdateUserRolesAsync(int userId, UpdateRolesRequest request);
}
