using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Trả dữ liệu phiếu sửa chữa cho Client[cite: 1, 2]
    /// </summary>
    public class RepairRequestResponseDto
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string IssueDescription { get; set; } = string.Empty;
        public string? TechnicalNote { get; set; }
        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? EstimatedReturnDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Thông tin Thiết bị mang đến sửa[cite: 1, 2]
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSerialNumber { get; set; } = string.Empty;

        // Thông tin Khách hàng sở hữu[cite: 1, 2]
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhoneNumber { get; set; } = string.Empty;

        // Thông tin Lễ tân tiếp nhận phiếu (null nếu Khách tự gửi online)[cite: 1, 2]
        public int? ReceptionistId { get; set; }
        public string? ReceptionistName { get; set; }

        // Thông tin Kỹ thuật viên phụ trách sửa chữa (null nếu chưa được phân công)[cite: 1, 2]
        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
    }
}