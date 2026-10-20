using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Поле Email є обов'язковим.")]
    [EmailAddress(ErrorMessage = "Некоректний формат Email адреси.")]
    [MaxLength(100, ErrorMessage = "Email не може перевищувати 100 символів.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Поле Пароль є обов'язковим.")]
    [MinLength(6, ErrorMessage = "Пароль повинен містити щонайменше 6 символів.")]
    [MaxLength(100, ErrorMessage = "Пароль не може перевищувати 100 символів.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Поле Ім'я (DisplayName) є обов'язковим.")]
    [MaxLength(50, ErrorMessage = "Ім'я не може перевищувати 50 символів.")]
    public string DisplayName { get; set; } = string.Empty;
}
