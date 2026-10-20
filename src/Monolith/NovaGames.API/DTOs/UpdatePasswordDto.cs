namespace NovaGames.API.DTOs
{
    // DTO для запиту на відправку коду
    public class UpdatePasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}