namespace NovaGames.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        string GenerateHtmlEmail(string title, string message, string? buttonText = null, string? buttonUrl = null);
    }
}