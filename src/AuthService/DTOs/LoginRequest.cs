using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Поле Логін/Email є обов'язковим.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Поле Пароль є обов'язковим.")]
    public string Password { get; set; } = string.Empty;
}
