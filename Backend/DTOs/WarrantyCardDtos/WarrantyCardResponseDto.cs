using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Trả dữ liệu thẻ bảo hành cho Client[cite: 1]
    /// </summary>
    public class WarrantyCardResponseDto
    {
        public int Id { get; set; }
        public string CardCode { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WarrantyType { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Thông tin thiết bị được gắn với thẻ này[cite: 1]
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSerialNumber { get; set; } = string.Empty;

        // Thông tin Khách hàng sở hữu[cite: 1]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Thẻ còn hiệu lực nếu IsActive = true và Ngày hiện tại <= EndDate[cite: 1]
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Số ngày bảo hành còn lại[cite: 1]
        /// </summary>
        public int DaysRemaining { get; set; }
    }
}