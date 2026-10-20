using NovaGames.Core.Interfaces;

namespace NovaGames.Core.Entities
{
    public class ShippingDetail : IEntity
    {
        public int Id { get; set; }
        public int OrderId { get; set; } // FK на замовлення
        public string City { get; set; } = string.Empty;
        public string PostOfficeAddress { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
    }
}