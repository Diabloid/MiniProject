using AuthService.DTOs;
using MiniProject.Shared.Common;

namespace AuthService.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequest request);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<Result> LogoutAsync(string refreshToken);
}
