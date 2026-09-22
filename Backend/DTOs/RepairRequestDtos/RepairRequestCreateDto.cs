using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Tạo phiếu bảo hành (Khách hàng tự tạo online hoặc Lễ tân tạo tại quầy)[cite: 1, 2]
    /// </summary>
    public class RepairRequestCreateDto
    {
        [Required(ErrorMessage = "Mã thiết bị (ProductId) không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId phải là số nguyên dương hợp lệ")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Mô tả sự cố/lỗi thiết bị không được để trống")]
        [MaxLength(500, ErrorMessage = "Mô tả sự cố tối đa 500 ký tự")]
        public string IssueDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã khách hàng (CustomerId) không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "CustomerId phải là số nguyên dương hợp lệ")]
        public int CustomerId { get; set; }

        /// <summary>
        /// Mã lễ tân lập phiếu (Chỉ truyền vào khi Lễ tân lập phiếu giúp Khách tại cửa hàng)[cite: 1, 2]
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "ReceptionistId phải là số nguyên dương hợp lệ")]
        public int? ReceptionistId { get; set; }
    }
}