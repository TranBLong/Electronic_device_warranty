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

        // Admin, Manager, Receptionist, Technician, Customer
        [Required, MaxLength(30)]
        public string Role { get; set; } = "Customer"; 

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (Quan hệ EF Core)
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<WarrantyTicket> CustomerTickets { get; set; } = new List<WarrantyTicket>();
        public ICollection<WarrantyTicket> ReceptionistTickets { get; set; } = new List<WarrantyTicket>();
        public ICollection<WarrantyTicket> TechnicianTickets { get; set; } = new List<WarrantyTicket>();
    }
}