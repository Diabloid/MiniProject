namespace NovaGames.API.DTOs
{
    public class UploadImageDto
    {
        public IFormFile File { get; set; } = null!;
        public string GameTitle { get; set; } = string.Empty;
        public string ImageType { get; set; } = string.Empty;
    }
}
