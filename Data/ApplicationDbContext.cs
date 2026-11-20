using Microsoft.EntityFrameworkCore;
using dailypracticeASPDotNet.Models.Domain;

namespace dailypracticeASPDotNet.Data
{
    /// <summary>
    /// ApplicationDbContext coordinates Entity Framework Core data access.
    /// Manages DbSets for Users and Roles, auto-incrementing integer identity keys, foreign keys, indexes, and role seeding.
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

            // Configure User entity with numeric integer primary key (auto-increment identity)
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).ValueGeneratedOnAdd();
                entity.HasIndex(u => u.EmailId).IsUnique();
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.MiddleName).HasMaxLength(50);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.EmailId).IsRequired().HasMaxLength(150);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.MobileNo).HasMaxLength(20);
                entity.Property(u => u.Address).HasMaxLength(250);

                // Binary Photo columns
                entity.Property(u => u.PhotoData); // Maps to VARBINARY(MAX) in SQL Server, BLOB in SQLite
                entity.Property(u => u.PhotoContentType).HasMaxLength(100);
                entity.Property(u => u.PhotoFileName).HasMaxLength(255);
                entity.Property(u => u.PhotoUrl).HasMaxLength(500);

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
        }
    }
}
