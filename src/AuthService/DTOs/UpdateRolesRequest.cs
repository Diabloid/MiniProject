using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

public class UpdateRolesRequest
{
    [Required(ErrorMessage = "Список ролей не може бути порожнім.")]
    [MinLength(1, ErrorMessage = "Вкажіть хоча б одну роль.")]
    public List<string> Roles { get; set; } = new();
}
