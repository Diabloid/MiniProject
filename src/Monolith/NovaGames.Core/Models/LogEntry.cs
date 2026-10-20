namespace NovaGames.Core.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } // Час створення логу
        public required string Level { get; set; }       // Рівень логу (INFO, WARNING, ERROR)
        public required string Message { get; set; }     // Повідомлення логу
        public required string CallerMethod { get; set; } // З якого методу викликали
        public required string CallerFile { get; set; }   // З якого файлу
    }
}