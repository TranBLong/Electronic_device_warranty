using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EWarrantySystem.Models
{
    public class WarrantyCard
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string CardCode { get; set; } = string.Empty; // Mã thẻ bảo hành

        public DateTime StartDate { get; set; } = DateTime.UtcNow; // Ngày bắt đầu / kích hoạt[cite: 2]

        public DateTime EndDate { get; set; } // Ngày hết hạn bảo hành[cite: 2, 3]

        [MaxLength(50)]
        public string WarrantyType { get; set; } = "Standard"; // Loại bảo hành (Chính hãng, Mở rộng, ...)

        public bool IsActive { get; set; } = true; // Trạng thái hiệu lực thẻ

        // Khóa ngoại liên kết 1-1 với Thiết bị
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}