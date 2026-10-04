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
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<RepairRequestPart> RepairRequestParts => Set<RepairRequestPart>();
    public DbSet<ServiceCenter> ServiceCenters => Set<ServiceCenter>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RepairRequestAttachment> RepairRequestAttachments => Set<RepairRequestAttachment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();

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
            entity.HasOne(user => user.ServiceCenter).WithMany(center => center.Staff)
                .HasForeignKey(user => user.ServiceCenterId).OnDelete(DeleteBehavior.Restrict);
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
            entity.HasOne(product => product.Category).WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(request => request.LaborCost).HasPrecision(18, 2);
            entity.HasOne(request => request.WarrantyCard).WithMany(card => card.RepairRequests)
                .HasForeignKey(request => request.WarrantyCardId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(request => request.Receptionist).WithMany(user => user.CreatedRepairRequests)
                .HasForeignKey(request => request.ReceptionistId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(request => request.Technician).WithMany(user => user.AssignedRepairRequests)
                .HasForeignKey(request => request.TechnicianId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(request => request.ServiceCenter).WithMany(center => center.RepairRequests)
                .HasForeignKey(request => request.ServiceCenterId).OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(500);
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(part => part.Id);
            entity.Property(part => part.Code).HasMaxLength(50).IsRequired();
            entity.Property(part => part.Name).HasMaxLength(200).IsRequired();
            entity.Property(part => part.Description).HasMaxLength(1000);
            entity.Property(part => part.UnitPrice).HasPrecision(18, 2);
            entity.HasIndex(part => part.Code).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Parts_UnitPrice", "[UnitPrice] >= 0");
                table.HasCheckConstraint("CK_Parts_StockQuantity", "[StockQuantity] >= 0");
            });
        });

        modelBuilder.Entity<RepairRequestPart>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.HasIndex(item => new { item.RepairRequestId, item.PartId }).IsUnique();
            entity.HasOne(item => item.RepairRequest).WithMany(request => request.Parts)
                .HasForeignKey(item => item.RepairRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Part).WithMany(part => part.RepairRequestParts)
                .HasForeignKey(item => item.PartId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_RepairRequestParts_Quantity", "[Quantity] > 0"));
        });

        modelBuilder.Entity<ServiceCenter>(entity =>
        {
            entity.HasKey(center => center.Id);
            entity.Property(center => center.Name).HasMaxLength(200).IsRequired();
            entity.Property(center => center.Address).HasMaxLength(500).IsRequired();
            entity.Property(center => center.Phone).HasMaxLength(30);
            entity.HasIndex(center => center.Name).IsUnique();
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(invoice => invoice.Id);
            entity.Property(invoice => invoice.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(invoice => invoice.PartsTotal).HasPrecision(18, 2);
            entity.Property(invoice => invoice.LaborTotal).HasPrecision(18, 2);
            entity.Property(invoice => invoice.TotalAmount).HasPrecision(18, 2);
            entity.Property(invoice => invoice.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();
            entity.HasIndex(invoice => invoice.RepairRequestId).IsUnique(); // 1 phiếu sửa - 1 hóa đơn
            entity.HasOne(invoice => invoice.RepairRequest).WithOne(request => request.Invoice)
                .HasForeignKey<Invoice>(invoice => invoice.RepairRequestId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_Invoices_TotalAmount", "[TotalAmount] >= 0"));
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(30);
            entity.Property(payment => payment.Note).HasMaxLength(500);
            entity.HasOne(payment => payment.Invoice).WithMany(invoice => invoice.Payments)
                .HasForeignKey(payment => payment.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(payment => payment.ReceivedBy).WithMany()
                .HasForeignKey(payment => payment.ReceivedById).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0"));
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(notification => notification.Title).HasMaxLength(200).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(1000).IsRequired();
            entity.HasIndex(notification => new { notification.UserId, notification.IsRead });
            entity.HasOne(notification => notification.User).WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(notification => notification.RepairRequest).WithMany(request => request.Notifications)
                .HasForeignKey(notification => notification.RepairRequestId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RepairRequestAttachment>(entity =>
        {
            entity.HasKey(attachment => attachment.Id);
            entity.Property(attachment => attachment.FileName).HasMaxLength(255).IsRequired();
            entity.Property(attachment => attachment.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(attachment => attachment.ContentType).HasMaxLength(100).IsRequired();
            entity.HasOne(attachment => attachment.RepairRequest).WithMany(request => request.Attachments)
                .HasForeignKey(attachment => attachment.RepairRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(attachment => attachment.UploadedBy).WithMany()
                .HasForeignKey(attachment => attachment.UploadedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(feedback => feedback.Id);
            entity.Property(feedback => feedback.Comment).HasMaxLength(1000);
            entity.HasIndex(feedback => feedback.RepairRequestId).IsUnique(); // 1 phiếu sửa - 1 đánh giá
            entity.HasOne(feedback => feedback.RepairRequest).WithOne(request => request.Feedback)
                .HasForeignKey<Feedback>(feedback => feedback.RepairRequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(feedback => feedback.Customer).WithMany(user => user.Feedbacks)
                .HasForeignKey(feedback => feedback.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_Feedbacks_Rating", "[Rating] BETWEEN 1 AND 5"));
        });
    }
}