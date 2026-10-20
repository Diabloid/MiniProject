using System.Text.Json;
using System.Runtime.CompilerServices;
using NovaGames.Core.Interfaces;
using NovaGames.Core.Models;

namespace NovaGames.Core.Services
{
    public class JsonLogger : ILogger
    {
        private readonly string _logFilePath;

        public JsonLogger()
        {
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string fileName = $"log_{DateTime.Now:yyyy-MM-dd}.json";
            _logFilePath = Path.Combine(directory, fileName);
        }

        // Реалізація методу з інтерфейсу
        public async Task LogAsync(string message, LogType type,
                                   [CallerMemberName] string callerName = "",
                                   [CallerFilePath] string filePath = "")
        {
            try
            {
                // 1. Створюємо об'єкт
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = type.ToString(),
                    Message = message,
                    CallerMethod = callerName,
                    CallerFile = Path.GetFileName(filePath)
                };

                // 2. Серіалізуємо в JSON
                string jsonString = JsonSerializer.Serialize(logEntry);

                // 3. Асинхронно дописуємо у файл
                await File.AppendAllTextAsync(_logFilePath, jsonString + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LOGGER FAILED: {ex.Message}");
            }
        }
    }
}