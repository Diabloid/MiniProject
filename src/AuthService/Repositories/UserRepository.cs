using AuthService.Data;
using AuthService.Entities;
using Microsoft.EntityFrameworkCore;
using MiniProject.Shared.Common;

namespace AuthService.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AuthDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<User?> GetUserWithRolesAsync(int id)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<PagedResult<User>> GetPagedUsersAsync(int page, int pageSize)
    {
        var totalCount = await _dbSet.CountAsync();

        var items = await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<User>(items, totalCount, page, pageSize);
    }

    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
    }

    public async Task<List<Role>> GetRolesByNamesAsync(IEnumerable<string> roleNames)
    {
        var lowerNames = roleNames.Select(r => r.ToLower()).ToList();
        return await _context.Roles
            .Where(r => lowerNames.Contains(r.Name.ToLower()))
            .ToListAsync();
    }

    public async Task UpdateRolesAsync(User user, IEnumerable<Role> newRoles)
    {
        var existingUserRoles = _context.UserRoles.Where(ur => ur.UserId == user.Id);
        _context.UserRoles.RemoveRange(existingUserRoles);

        foreach (var role in newRoles)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        await _context.SaveChangesAsync();
    }
}
