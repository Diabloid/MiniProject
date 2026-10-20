namespace NovaGames.Core.Entities
{
    public class SubscriptionGame : Game
    {
        public int DurationMonths { get; set; } // Тривалість підписки в місяцях
        public bool IsAutoRenew { get; set; }   // Чи є автоматичне подовження
    }
}