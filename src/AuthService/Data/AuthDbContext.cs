using AuthService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Налаштування User
            builder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique(); // Унікальний індекс
                entity.Property(u => u.Email).HasMaxLength(100).IsRequired();
                entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });

            // Налаштування зв'язку багато-до-багатьох (UserRole)
            builder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId }); // Композитний ключ

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId);

                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId);
            });

            // Налаштування RefreshToken
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(rt => rt.Token).IsUnique();
                entity.Property(rt => rt.Token).HasMaxLength(200).IsRequired();
                entity.Property(rt => rt.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });

            // --- SEED ДАНИХ ---

            // 1. Створюємо ролі
            var userRole = new Role { Id = 1, Name = "User" };
            var adminRole = new Role { Id = 2, Name = "Admin" };
            builder.Entity<Role>().HasData(userRole, adminRole);

            // 2. Створюємо першого адміна (пароль: Admin123!)
            // Хеш згенеровано заздалегідь через BCrypt, щоб не тягнути логіку хешування в DbContext
            var adminId = 1;
            builder.Entity<User>().HasData(new User
            {
                Id = adminId,
                Email = "admin@novagames.com",
                PasswordHash = "$2a$11$wE56F1.T.5C/O/oZ1PzD/u9yN9hV8eB7x5ZJ9H5g/j5f6z5ZJ9H5g", // Замінити на реальний хеш при потребі
                DisplayName = "SuperAdmin",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Фіксована дата для Seed
            });

            // 3. Прив'язуємо адміна до ролі Admin
            builder.Entity<UserRole>().HasData(new UserRole
            {
                UserId = adminId,
                RoleId = adminRole.Id
            });
        }
    }
}