namespace NovaGames.Core.Entities
{
    public class PhysicalGame : Game
    {
        public double ShippingWeight { get; set; }   // Вага
        public int? StockQuantity { get; set; }      // Кількість на складі
    }
}