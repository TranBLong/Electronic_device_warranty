using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EWarrantySystem.Models
{
    public class RepairRequest
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string RequestCode { get; set; } = string.Empty; // Mã phiếu (ví dụ: REQ-20260920-001)

        [Required, MaxLength(500)]
        public string IssueDescription { get; set; } = string.Empty; // Mô tả sự cố/lỗi thiết bị[cite: 2, 3]

        public string? TechnicalNote { get; set; } // Ghi chú đánh giá của Kỹ thuật viên

        public RepairStatusEnum Status { get; set; } = RepairStatusEnum.Pending; // Trạng thái xử lý[cite: 2, 3]

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Ngày tiếp nhận/tạo phiếu[cite: 2, 3]

        public DateTime? EstimatedReturnDate { get; set; } // Ngày hẹn trả máy cho khách[cite: 2, 3]

        public DateTime? CompletedAt { get; set; } // Ngày sửa xong hoàn tất[cite: 2]

        [MaxLength(500)]
        public string? EvidenceImageUrl { get; set; }  // đường dẫn ảnh lưu trên server

        // ============================================================
        // FOREIGN KEYS & NAVIGATION PROPERTIES[cite: 2, 3]
        // ============================================================

        // Thiết bị cần sửa
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        // Khách hàng gửi yêu cầu
        public int CustomerId { get; set; }
        public User? Customer { get; set; }

        // Nhân viên tiếp nhận (Có thể null nếu khách tự gửi online)
        public int? ReceptionistId { get; set; }
        public User? Receptionist { get; set; }

        // Kỹ thuật viên phụ trách sửa chữa (Null trước khi được phân công)
        public int? TechnicianId { get; set; }
        public User? Technician { get; set; }
    }
}