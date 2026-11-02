using Microsoft.EntityFrameworkCore;
using OrderingService.Entities;

namespace OrderingService.Data;

public class OrderingDbContext : DbContext
{
    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.UserId).IsRequired();
            entity.Property(o => o.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(o => o.TotalAmount)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(o => o.PaymentMethod)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(o => o.CustomerNotes)
                  .HasMaxLength(500);

            entity.Property(o => o.IdempotencyKey)
                  .HasMaxLength(100);

            entity.Property(o => o.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.Status);
            entity.HasIndex(o => o.IdempotencyKey).IsUnique();

            entity.HasMany(o => o.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.GameId).IsRequired();
            entity.Property(i => i.Title).HasMaxLength(200).IsRequired();
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(i => i.Quantity).HasDefaultValue(1);

            entity.HasIndex(i => i.OrderId);
            entity.HasIndex(i => i.GameId);
        });

        // Seed тестове замовлення
        var initialOrder = new Order
        {
            Id = 1,
            UserId = 1,
            Status = OrderStatus.Completed,
            TotalAmount = 1019.15m,
            PaymentMethod = "Card",
            CustomerNotes = "Термінова доставка ліцензійного ключа",
            IdempotencyKey = "7b9f3a1e-842c-4c2e-9d21-f8a12c3e4567",
            CreatedAt = new DateTime(2026, 11, 2, 18, 0, 0, DateTimeKind.Utc)
        };

        var initialItem = new OrderItem
        {
            Id = 1,
            OrderId = 1,
            GameId = 1,
            Title = "Cyberpunk 2077: Phantom Liberty",
            UnitPrice = 1019.15m,
            Quantity = 1
        };

        builder.Entity<Order>().HasData(initialOrder);
        builder.Entity<OrderItem>().HasData(initialItem);
    }
}
