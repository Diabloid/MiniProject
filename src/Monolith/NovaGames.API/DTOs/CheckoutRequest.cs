public class CheckoutRequest
{
    public string PaymentMethod { get; set; } = "Wallet"; // "Wallet" або "Card"

    // Дані картки (потрібні тільки якщо PaymentMethod == "Card")
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public bool SaveCard { get; set; }
}