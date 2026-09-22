using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Gia hạn / Đổi loại bảo hành / Vô hiệu hóa thẻ[cite: 1]
    /// </summary>
    public class WarrantyCardUpdateDto
    {
        [Required(ErrorMessage = "Ngày hết hạn bảo hành mới không được để trống")]
        [DataType(DataType.Date, ErrorMessage = "Ngày hết hạn không đúng định dạng")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Loại bảo hành không được để trống")]
        [MaxLength(50, ErrorMessage = "Loại bảo hành tối đa 50 ký tự")]
        [RegularExpression(@"^(Standard|Extended|VIP|Official)$", ErrorMessage = "Loại bảo hành không hợp lệ (Standard, Extended, VIP, Official)")]
        public string WarrantyType { get; set; } = "Standard";

        [Required(ErrorMessage = "Vui lòng chọn trạng thái hiệu lực thẻ (IsActive)")]
        public bool IsActive { get; set; }
    }
}