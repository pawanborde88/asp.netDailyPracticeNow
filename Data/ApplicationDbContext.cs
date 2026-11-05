using Microsoft.EntityFrameworkCore;
using dailypracticeASPDotNet.Models.Domain;

namespace dailypracticeASPDotNet.Data
{
    /// <summary>
    /// ApplicationDbContext coordinates Entity Framework Core data access.
    /// Manages DbSets for Users and Roles, foreign keys, indexes, and initial data seeding.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Role entity
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Description).HasMaxLength(200);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.EmailId).IsUnique();
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.MiddleName).HasMaxLength(50);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.EmailId).IsRequired().HasMaxLength(150);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.MobileNo).HasMaxLength(20);
                entity.Property(u => u.Address).HasMaxLength(250);

                // Foreign Key relationship to Role
                entity.HasOne(u => u.Role)
                      .WithMany()
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // SEEDING DATA USING ENTITY FRAMEWORK CORE
            // ==========================================

            // 1. Seed Roles
            var adminRole = new Role { Id = 1, Name = "Admin", Description = "System Administrator with full access" };
            var userRole = new Role { Id = 2, Name = "User", Description = "Standard registered practice user" };
            var managerRole = new Role { Id = 3, Name = "Manager", Description = "Management user with elevated access" };

            modelBuilder.Entity<Role>().HasData(adminRole, userRole, managerRole);

            // 2. Seed Default Users
            // Verified BCrypt Hash for "Password@123"
            const string defaultPasswordHash = "$2a$11$OsPalY7ze9VsBPps3J55Zu5sTbv3VjvgRxWSWLS7ktZ1J5M4WZXDe";

            var adminUser = new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FirstName = "System",
                MiddleName = "Super",
                LastName = "Admin",
                EmailId = "admin@example.com",
                MobileNo = "9876543210",
                Address = "HQ Office, Mumbai, India",
                RoleId = 1,
                PasswordHash = defaultPasswordHash,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var practiceUser = new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FirstName = "Pawan",
                MiddleName = "",
                LastName = "Borde",
                EmailId = "user@example.com",
                MobileNo = "9123456780",
                Address = "Pune, Maharashtra, India",
                RoleId = 2,
                PasswordHash = defaultPasswordHash,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            modelBuilder.Entity<User>().HasData(adminUser, practiceUser);
        }
    }
}
