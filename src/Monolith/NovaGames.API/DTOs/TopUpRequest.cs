namespace NovaGames.API.DTOs
{
    public class TopUpRequest
    {
        public decimal Amount { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public bool SaveCard { get; set; }
    }
}