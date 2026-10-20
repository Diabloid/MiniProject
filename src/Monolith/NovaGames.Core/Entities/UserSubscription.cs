using NovaGames.Core.Interfaces;
using System;

namespace NovaGames.Core.Entities
{
    public class UserSubscription : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool AutoRenew { get; set; }
    }
}