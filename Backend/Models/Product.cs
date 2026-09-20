using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EWarrantySystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty; // Mã Serial / IMEI độc nhất[cite: 2, 3]

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty; // Tên thiết bị[cite: 2, 3]

        [MaxLength(100)]
        public string Model { get; set; } = string.Empty; // Dòng máy / Model

        [MaxLength(100)]
        public string Brand { get; set; } = string.Empty; // Thương hiệu

        public DateTime PurchaseDate { get; set; } // Ngày mua[cite: 2, 3]

        // Khóa ngoại liên kết với Khách hàng sở hữu[cite: 2, 3]
        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public User? Customer { get; set; }

        // Navigation Properties
        public WarrantyCard? WarrantyCard { get; set; } // Thẻ bảo hành đính kèm
        public ICollection<RepairRequest> RepairRequests { get; set; } = new List<RepairRequest>(); // Lịch sử sửa chữa
    }
}