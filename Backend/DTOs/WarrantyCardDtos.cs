using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// Lớp chứa tất cả các DTOs liên quan đến Phân hệ Quản lý Thẻ / Sổ bảo hành (WarrantyCard)
    /// </summary>
    public class WarrantyCardDtos
    {
        // =========================================================================
        // 1. DTO TẠO / KÍCH HOẠT THẺ BẢO HÀNH (Bắt buộc)
        // =========================================================================
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

        // =========================================================================
        // 2. DTO GIA HẠN / ĐỔI LOẠI BẢO HÀNH / VÔ HIỆU HÓA THẺ (Bắt buộc)
        // =========================================================================
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

        // =========================================================================
        // 3. DTO TRẢ DỮ LIỆU THẺ BẢO HÀNH CHO CLIENT (Bắt buộc)
        // =========================================================================
        public class WarrantyCardResponseDto
        {
            public int Id { get; set; }
            public string CardCode { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string WarrantyType { get; set; } = string.Empty;
            public bool IsActive { get; set; }

            // Thông tin thiết bị được gắn với thẻ này
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string ProductSerialNumber { get; set; } = string.Empty;

            // Thông tin Khách hàng sở hữu
            public string CustomerName { get; set; } = string.Empty;

            // Trường tính toán sẵn giúp Client kiểm tra nhanh
            /// <summary>
            /// Thẻ còn hiệu lực nếu IsActive = true và Ngày hiện tại <= EndDate
            /// </summary>
            public bool IsValid { get; set; }

            /// <summary>
            /// Số ngày bảo hành còn lại
            /// </summary>
            public int DaysRemaining { get; set; }
        }
    }
}