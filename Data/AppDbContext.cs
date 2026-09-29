using Microsoft.EntityFrameworkCore;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<WarrantyCard> WarrantyCards => Set<WarrantyCard>();
    public DbSet<RepairRequest> RepairRequests => Set<RepairRequest>();
    public DbSet<RepairRequestStatusHistory> RepairRequestStatusHistories => Set<RepairRequestStatusHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Phone).HasMaxLength(30);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Brand).HasMaxLength(100).IsRequired();
            entity.Property(product => product.Model).HasMaxLength(100).IsRequired();
            entity.Property(product => product.SerialNumber).HasMaxLength(100).IsRequired();
            entity.HasIndex(product => product.SerialNumber).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint("CK_Products_WarrantyMonths", "[WarrantyMonths] > 0"));
        });

        modelBuilder.Entity<WarrantyCard>(entity =>
        {
            entity.HasKey(card => card.Id);
            entity.Property(card => card.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(card => card.Product).WithMany(product => product.WarrantyCards)
                .HasForeignKey(card => card.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(card => card.Customer).WithMany(user => user.WarrantyCards)
                .HasForeignKey(card => card.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_WarrantyCards_DateRange", "[EndDate] >= [StartDate]"));
        });

        modelBuilder.Entity<RepairRequest>(entity =>
        {
            entity.HasKey(request => request.Id);
            entity.Property(request => request.Description).HasMaxLength(4000).IsRequired();
            entity.Property(request => request.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(request => request.WarrantyCard).WithMany(card => card.RepairRequests)
                .HasForeignKey(request => request.WarrantyCardId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(request => request.Receptionist).WithMany(user => user.CreatedRepairRequests)
                .HasForeignKey(request => request.ReceptionistId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(request => request.Technician).WithMany(user => user.AssignedRepairRequests)
                .HasForeignKey(request => request.TechnicianId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RepairRequestStatusHistory>(entity =>
        {
            entity.HasKey(history => history.Id);
            entity.Property(history => history.OldStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(history => history.RepairRequest).WithMany(request => request.StatusHistory)
                .HasForeignKey(history => history.RepairRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(history => history.User).WithMany(user => user.RepairRequestStatusChanges)
                .HasForeignKey(history => history.ChangedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Token).HasMaxLength(64).IsRequired();
            entity.HasIndex(token => token.Token).IsUnique();
            entity.HasOne(token => token.User).WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}