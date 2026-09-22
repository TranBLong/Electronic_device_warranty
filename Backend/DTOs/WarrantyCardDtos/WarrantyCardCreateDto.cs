using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Tạo / Kích hoạt thẻ bảo hành[cite: 1]
    /// </summary>
    public class WarrantyCardCreateDto
    {
        [Required(ErrorMessage = "Mã thẻ bảo hành không được để trống")]
        [MaxLength(50, ErrorMessage = "Mã thẻ bảo hành tối đa 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Mã thẻ chỉ được chứa chữ cái, chữ số, dấu gạch ngang và gạch dưới")]
        public string CardCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày bắt đầu bảo hành không được để trống")]
        [DataType(DataType.Date, ErrorMessage = "Ngày bắt đầu không đúng định dạng")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Ngày hết hạn bảo hành không được để trống")]
        [DataType(DataType.Date, ErrorMessage = "Ngày hết hạn không đúng định dạng")]
        public DateTime EndDate { get; set; }

        [MaxLength(50, ErrorMessage = "Loại bảo hành tối đa 50 ký tự")]
        [RegularExpression(@"^(Standard|Extended|VIP|Official)$", ErrorMessage = "Loại bảo hành không hợp lệ (Standard, Extended, VIP, Official)")]
        public string WarrantyType { get; set; } = "Standard";

        [Required(ErrorMessage = "Mã thiết bị (ProductId) không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId phải là số nguyên dương hợp lệ")]
        public int ProductId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}