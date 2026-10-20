namespace NovaGames.API.DTOs
{
    public class LibraryGameDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Category { get; set; } = "Без категорії";
        public bool IsInstalled { get; set; } = false;
        public double AverageRating { get; set; } = 0.0;
        public int TotalReviews { get; set; } = 0;
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTime AcquiredAt { get; set; }
        public double SizeGB { get; set; }
        public DateTime? LastPlayedAt { get; set; }
        public string HeroImageUrl { get; set; } = string.Empty;
        public List<string> ScreenshotPaths { get; set; } = new List<string>();
        public long PlaytimeSeconds { get; set; } = 0;
    }
}
