using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    // =========================================================================
    // 3. DTO TRẢ DỮ LIỆU THIẾT BỊ CHO CLIENT (Bắt buộc)
    // =========================================================================
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }

        // Thông tin Khách hàng sở hữu
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhoneNumber { get; set; } = string.Empty;

        // Thông tin Thẻ bảo hành đính kèm (nếu có)
        public int? WarrantyCardId { get; set; }
        public string? WarrantyCardCode { get; set; }
        public DateTime? WarrantyEndDate { get; set; }
        
        /// <summary>
        /// Trạng thái thiết bị còn hiệu lực bảo hành hay không
        /// </summary>
        public bool IsUnderWarranty { get; set; }
    }
}
