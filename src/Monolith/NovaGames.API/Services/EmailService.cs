using System.Net;
using System.Net.Mail;

namespace NovaGames.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // Беремо налаштування з appsettings.json
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:Port"]!);
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderPassword = _config["EmailSettings:SenderPassword"];

            var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage(from: senderEmail!, to: toEmail, subject, message)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
        }

        // МЕТОД-ГЕНЕРАТОР ОФІЦІЙНОГО ШАБЛОНУ NOVAGAMES
        public string GenerateHtmlEmail(string title, string message, string? buttonText = null, string? buttonUrl = null)
        {
            // Формуємо кнопку ТІЛЬКИ якщо передані дані
            string buttonHtml = "";
            if (!string.IsNullOrEmpty(buttonText) && !string.IsNullOrEmpty(buttonUrl))
            {
                buttonHtml = $@"
                        <div class='btn-container'>
                            <a href='{buttonUrl}' class='btn'>{buttonText}</a>
                        </div>";
            }

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #121212; margin: 0; padding: 20px; }}
                    .container {{ max-width: 600px; margin: 0 auto; background-color: #1A1A1A; border: 1px solid #333333; border-radius: 8px; overflow: hidden; }}
                    .header {{ background-color: #252525; padding: 25px; text-align: center; border-bottom: 2px solid #00ADEF; }}
                    .header h1 {{ margin: 0; color: #00ADEF; font-size: 26px; letter-spacing: 3px; font-weight: 800; }}
                    .content {{ padding: 40px 30px; text-align: center; }}
                    .content h2 {{ color: #ffffff; margin-top: 0; font-size: 22px; margin-bottom: 20px; }}
                    .content p {{ font-size: 16px; color: #DDDDDD; line-height: 1.6; margin-bottom: 35px; }}
                    .btn-container {{ text-align: center; margin-bottom: 10px; }}
                    .btn {{ display: inline-block; padding: 14px 30px; background-color: #00ADEF; color: #ffffff !important; text-decoration: none; font-weight: bold; border-radius: 6px; font-size: 16px; text-transform: uppercase; letter-spacing: 1px; }}
                    .footer {{ background-color: #181818; padding: 20px; text-align: center; font-size: 12px; color: #666666; border-top: 1px solid #333333; line-height: 1.5; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>NOVAGAMES</h1>
                    </div>
                    <div class='content'>
                        <h2>{title}</h2>
                        <p>{message}</p>
                        {buttonHtml}
                    </div>
                    <div class='footer'>
                        <p>Ви отримали цей лист, оскільки ваш email був вказаний при реєстрації або відновленні пароля у додатку NovaGames.<br>
                        Якщо ви не створювали цей запит, просто проігноруйте дане повідомлення.</p>
                        <p>&copy; {DateTime.Now.Year} NovaGames. Всі права захищені.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}