using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Поле Email є обов'язковим.")]
    [EmailAddress(ErrorMessage = "Некоректний формат Email адреси.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Поле Пароль є обов'язковим.")]
    public string Password { get; set; } = string.Empty;
}
