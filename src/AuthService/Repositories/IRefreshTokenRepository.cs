using AuthService.Entities;

namespace AuthService.Repositories;

public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenWithUserAsync(string token);
    Task RevokeUserTokensAsync(int userId);
}
