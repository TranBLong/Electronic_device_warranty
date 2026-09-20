using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EWarrantySystem.Models
{
    public class WarrantyTicket
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TicketCode { get; set; } = string.Empty; // Mã phiếu (VD: W-202609001)

        [Required, MaxLength(500)]
        public string IssueDescription { get; set; } = string.Empty; // Mô tả sự cố từ khách

        [MaxLength(500)]
        public string? InspectionNotes { get; set; } // Ghi chú kiểm tra của kỹ thuật viên

        // Trạng thái phiếu: Pending (Chờ), Processing (Đang sửa), Completed (Hoàn thành), Delivered (Đã trả khách)
        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EstimatedCompletionDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Khóa ngoại đến Sản phẩm
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        // Khóa ngoại đến Khách hàng (Customer)
        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public User? Customer { get; set; }

        // Khóa ngoại đến Nhân viên tiếp nhận (Receptionist)
        public int? ReceptionistId { get; set; }
        [ForeignKey(nameof(ReceptionistId))]
        public User? Receptionist { get; set; }

        // Khóa ngoại đến Kỹ thuật viên sửa chữa (Technician)
        public int? TechnicianId { get; set; }
        [ForeignKey(nameof(TechnicianId))]
        public User? Technician { get; set; }

        // Danh sách linh kiện đã sử dụng cho phiếu này
        public ICollection<TicketPart> TicketParts { get; set; } = new List<TicketPart>();
    }
}