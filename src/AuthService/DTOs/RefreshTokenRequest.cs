using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Поле RefreshToken є обов'язковим.")]
    public string RefreshToken { get; set; } = string.Empty;
}
