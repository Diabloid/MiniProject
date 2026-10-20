namespace NovaGames.API.DTOs
{
    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Text { get; set; }
    }
}