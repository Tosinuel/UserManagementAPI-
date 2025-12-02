using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Models;

namespace UserManagementAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Models.AppUser> AppUsers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().HasKey(u => u.Id);
            modelBuilder.Entity<User>().Property(u => u.FirstName).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<User>().Property(u => u.LastName).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<User>().Property(u => u.Email).HasMaxLength(200).IsRequired();
            modelBuilder.Entity<Models.AppUser>().HasKey(u => u.Id);
            modelBuilder.Entity<Models.AppUser>().HasIndex(u => u.Username).IsUnique();
        }
    }
}
