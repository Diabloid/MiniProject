using AuthService.Entities;

namespace AuthService.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpiration();
    DateTime GetRefreshTokenExpiration();
}
