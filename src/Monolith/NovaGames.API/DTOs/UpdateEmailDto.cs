namespace NovaGames.API.DTOs
{
    // DTO для запиту на відправку коду
    public class SendEmailCodeDto
    {
        public string Email { get; set; } = string.Empty;
    }

    // DTO для фінальної прив'язки
    public class UpdateEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}