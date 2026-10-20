using AuthService.Entities;

namespace AuthService.Repositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email);
        Task<User?> GetUserWithRolesAsync(int id);
    }
}