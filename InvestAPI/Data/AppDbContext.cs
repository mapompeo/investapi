using InvestAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<Assets> Assets { get; set; }
        public DbSet<Transactions> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            });

            modelBuilder.Entity<Assets>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.Ticker }).IsUnique();
                entity.Property(e => e.Ticker).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Quantity).HasPrecision(18, 8);
                entity.Property(e => e.AvgBuyPrice).HasPrecision(18, 2);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Assets)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Transactions>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantity).HasPrecision(18, 8);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.TotalValue).HasPrecision(18, 2);
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasOne(e => e.Assets)
                    .WithMany(a => a.Transactions)
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}