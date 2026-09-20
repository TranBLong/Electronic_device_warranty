using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Address { get; set; }

        // Mật khẩu mã hóa
        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Required]
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Phân biệt 5 vai trò: "Admin", "Manager", "Receptionist", "Technician", "Customer"[cite: 1, 2, 3, 4]
        /// </summary>
        [Required, MaxLength(30)]
        public string Role { get; set; } = "Customer";[cite: 2, 3, 4]

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ============================================================
        // NAVIGATION PROPERTIES (Quan hệ CSDL trong Entity Framework)[cite: 2]
        // ============================================================

        // Danh sách thiết bị sở hữu (Dành cho Customer)[cite: 2, 3]
        public ICollection<Product> Products { get; set; } = new List<Product>();

        // Danh sách phiếu yêu cầu sửa chữa (Dành cho Customer)[cite: 2, 3]
        public ICollection<RepairRequest> CustomerRequests { get; set; } = new List<RepairRequest>();

        // Danh sách phiếu đã tiếp nhận (Dành cho Receptionist)[cite: 2, 3]
        public ICollection<RepairRequest> ReceptionistRequests { get; set; } = new List<RepairRequest>();

        // Danh sách phiếu được giao sửa chữa (Dành cho Technician)[cite: 2, 3]
        public ICollection<RepairRequest> TechnicianRequests { get; set; } = new List<RepairRequest>();
    }
}