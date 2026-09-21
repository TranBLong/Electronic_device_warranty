using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Models; // Đổi theo namespace chứa các Model của bạn

namespace EWarrantySystem.Data // Đổi theo namespace thư mục Data của bạn
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<WarrantyCard> WarrantyCards { get; set; } = null!;
        public DbSet<RepairRequest> RepairRequests { get; set; } = null!;
    }
}