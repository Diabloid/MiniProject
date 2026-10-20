using CatalogService.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<GameItem> Items { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(500);
        });

        builder.Entity<GameItem>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Title).HasMaxLength(200).IsRequired();
            entity.Property(g => g.Description).HasMaxLength(2000).IsRequired();
            entity.Property(g => g.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(g => g.Discount).HasPrecision(5, 2).HasDefaultValue(0);
            entity.Property(g => g.ItemType).HasMaxLength(50).IsRequired();
            entity.Property(g => g.CoverUrl).HasMaxLength(500);
            entity.Property(g => g.IconUrl).HasMaxLength(500);
            entity.Property(g => g.GenresJson).HasColumnType("longtext");
            entity.Property(g => g.IsActive).HasDefaultValue(true);
            entity.Property(g => g.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            entity.HasIndex(g => g.Title);
            entity.HasIndex(g => new { g.IsActive, g.CategoryId });

            entity.HasOne(g => g.Category)
                  .WithMany(c => c.Items)
                  .HasForeignKey(g => g.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        var actionCat = new Category { Id = 1, Name = "Action", Description = "Динамічні екшн-ігри" };
        var rpgCat = new Category { Id = 2, Name = "RPG", Description = "Рольові ігри з глибоким сюжетом" };
        var strategyCat = new Category { Id = 3, Name = "Strategy", Description = "Тактичні та стратегічні ігри" };
        var indieCat = new Category { Id = 4, Name = "Indie", Description = "Ігри від незалежних розробників" };

        builder.Entity<Category>().HasData(actionCat, rpgCat, strategyCat, indieCat);

        builder.Entity<GameItem>().HasData(
            new GameItem
            {
                Id = 1,
                Title = "Cyberpunk 2077: Phantom Liberty",
                Description = "Пригодницький рольовий бойовик у відкритому світі темного майбутнього.",
                Price = 1199.00m,
                Discount = 15.00m,
                CategoryId = 2,
                ItemType = "Digital",
                CoverUrl = "/Assets/default_game.png",
                IconUrl = "/Assets/default_icon.png",
                GenresJson = "[\"RPG\", \"Sci-Fi\", \"Action\"]",
                DownloadSize = 70.5,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new GameItem
            {
                Id = 2,
                Title = "The Witcher 3: Wild Hunt - Complete Edition",
                Description = "Сюжетна рольова гра нового покоління у фантастичному відкритому всесвіті.",
                Price = 799.00m,
                Discount = 50.00m,
                CategoryId = 2,
                ItemType = "Digital",
                CoverUrl = "/Assets/default_game.png",
                IconUrl = "/Assets/default_icon.png",
                GenresJson = "[\"RPG\", \"Open World\", \"Fantasy\"]",
                DownloadSize = 50.0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new GameItem
            {
                Id = 3,
                Title = "DOOM Eternal: Deluxe Edition",
                Description = "Армії пекла вторглися на Землю. Станьте Катом Року у битві за порятунок людства.",
                Price = 999.00m,
                Discount = 0.00m,
                CategoryId = 1,
                ItemType = "Physical",
                CoverUrl = "/Assets/default_game.png",
                IconUrl = "/Assets/default_icon.png",
                GenresJson = "[\"Action\", \"Shooter\", \"First-Person\"]",
                StockQuantity = 25,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new GameItem
            {
                Id = 4,
                Title = "Hollow Knight: Silksong",
                Description = "Досліджуйте розкішне королівство, де панують шовк та пісня.",
                Price = 450.00m,
                Discount = 0.00m,
                CategoryId = 4,
                ItemType = "Digital",
                CoverUrl = "/Assets/default_game.png",
                IconUrl = "/Assets/default_icon.png",
                GenresJson = "[\"Indie\", \"Metroidvania\", \"Souls-like\"]",
                DownloadSize = 8.5,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
