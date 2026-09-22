using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    // =========================================================================
    // 1. DTO THÊM THIẾT BỊ MỚI (Bắt buộc)
    // =========================================================================
    public class ProductCreateDto
    {
        [Required(ErrorMessage = "Mã Serial/IMEI không được để trống")]
        [MaxLength(100, ErrorMessage = "Mã Serial/IMEI tối đa 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Mã Serial/IMEI chỉ được chứa chữ cái, chữ số, dấu gạch ngang và gạch dưới")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên thiết bị không được để trống")]
        [MaxLength(150, ErrorMessage = "Tên thiết bị tối đa 150 ký tự")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int CustomerId { get; set; }
    }
}
