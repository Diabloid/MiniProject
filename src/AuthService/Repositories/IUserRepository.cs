using AuthService.Entities;
using MiniProject.Shared.Common;

namespace AuthService.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User?> GetUserWithRolesAsync(int id);
    Task<PagedResult<User>> GetPagedUsersAsync(int page, int pageSize);
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task<List<Role>> GetRolesByNamesAsync(IEnumerable<string> roleNames);
    Task UpdateRolesAsync(User user, IEnumerable<Role> newRoles);
}
