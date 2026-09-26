using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Models;

namespace EWarrantySystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<WarrantyCard> WarrantyCards { get; set; } = null!;
        public DbSet<RepairRequest> RepairRequests { get; set; } = null!;
        public DbSet<RepairRequestStatusHistory> RepairRequestStatusHistories { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique indexes
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.SerialNumber).IsUnique();
            modelBuilder.Entity<WarrantyCard>().HasIndex(w => w.CardCode).IsUnique();
            modelBuilder.Entity<RepairRequest>().HasIndex(r => r.RequestCode).IsUnique();

            // Ánh xạ các khóa ngoại đến User trong RepairRequest
            modelBuilder.Entity<RepairRequest>(entity =>
            {
                entity.HasOne(r => r.Customer)
                      .WithMany(u => u.CustomerRequests)
                      .HasForeignKey(r => r.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Receptionist)
                      .WithMany(u => u.ReceptionistRequests)
                      .HasForeignKey(r => r.ReceptionistId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Technician)
                      .WithMany(u => u.TechnicianRequests)
                      .HasForeignKey(r => r.TechnicianId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Tắt Cascade Delete giữa RepairRequestStatusHistory và User để tránh lỗi SQL 1785
            modelBuilder.Entity<RepairRequestStatusHistory>(entity =>
            {
                entity.HasOne(h => h.ChangedByUser)
                      .WithMany()
                      .HasForeignKey(h => h.ChangedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}