namespace NovaGames.Core.Entities
{
    // 1. Структура для одного набору вимог
    public class SystemRequirement
    {
        public string OS { get; set; } = string.Empty;
        public string Processor { get; set; } = string.Empty;
        public string Memory { get; set; } = string.Empty;
        public string Graphics { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
    }

    // 2. Головний об'єкт, який буде зберігатись у JSON
    public class GameRequirements
    {
        public SystemRequirement Minimum { get; set; } = new SystemRequirement();
        public SystemRequirement Recommended { get; set; } = new SystemRequirement();
    }

    public class DigitalGame : Game
    {
        public double DownloadSize { get; set; }
        public string GameZipUrl { get; set; } = string.Empty; // Сюди ми запишемо шлях до .zip файлу
        public GameRequirements Requirements { get; set; } = new GameRequirements();
    }
}