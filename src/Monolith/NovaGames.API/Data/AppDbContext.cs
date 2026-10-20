using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NovaGames.Core.Entities;
using System.Text.Json;

namespace NovaGames.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Таблиці
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShippingDetail> ShippingDetails { get; set; }
        public DbSet<UserLibrary> UserLibraries { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SavedCard> SavedCards { get; set; }
        public DbSet<Review> Reviews { get; set; }

        // Правила
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Композитний ключ для Бібліотеки
            modelBuilder.Entity<UserLibrary>()
                .HasKey(ul => new { ul.UserId, ul.ProductId });

            // 2. Налаштування таблиці Ігор
            modelBuilder.Entity<Game>(entity =>
            {
                // Точність для грошей
                entity.Property(g => g.Price).HasColumnType("decimal(18,2)");

                // Зберігаємо медіа як JSON-рядок
                entity.Property(g => g.Media)
                      .HasConversion(
                          media => JsonSerializer.Serialize(media, new JsonSerializerOptions()),
                          json => JsonSerializer.Deserialize<GameMedia>(json, new JsonSerializerOptions())!)
                      .HasColumnType("json");

                // Патерн TPH (Table-Per-Hierarchy)
                entity.HasDiscriminator<string>("ProductType")
                      .HasValue<DigitalGame>("Digital")
                      .HasValue<PhysicalGame>("Physical")
                      .HasValue<SubscriptionGame>("Subscription");
                var genreComparer = new ValueComparer<List<string>>(
                     (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                     c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                     c => c.ToList());

                entity.Property(g => g.Genres)
                      .HasConversion(
                          v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                          v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions())!)
                      .HasColumnType("json")
                      .Metadata.SetValueComparer(genreComparer);
                modelBuilder.Entity<DigitalGame>(entity =>
                {
                    entity.Property(d => d.Requirements)
                          .HasConversion(
                              req => JsonSerializer.Serialize(req, new JsonSerializerOptions()),
                              json => string.IsNullOrEmpty(json) ? new GameRequirements() : JsonSerializer.Deserialize<GameRequirements>(json, new JsonSerializerOptions())!)
                          .HasColumnType("json");
                });
            });

            // Налаштування таблиці Збережених карток
            modelBuilder.Entity<SavedCard>()
                .HasOne(c => c.User)
                .WithMany(u => u.SavedCards)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade); // При видаленні юзера видаляємо його картки

            // 3. Точність для інших полів
            modelBuilder.Entity<User>()
                .Property(u => u.WalletBalance).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.PriceAtPurchase).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Review>()
                .ToTable("Reviews", t => t.HasCheckConstraint("CK_Review_Rating", "Rating >= 1 AND Rating <= 5"));
        }
    }
}