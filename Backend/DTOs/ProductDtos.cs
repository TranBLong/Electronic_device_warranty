using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// Lớp chứa tất cả các DTOs liên quan đến Phân hệ Quản lý Thiết bị / Sản phẩm (Product)
    /// </summary>
    public class ProductDtos
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

            [MaxLength(100, ErrorMessage = "Model/Dòng máy tối đa 100 ký tự")]
            public string Model { get; set; } = string.Empty;

            [MaxLength(100, ErrorMessage = "Thương hiệu tối đa 100 ký tự")]
            public string Brand { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ngày mua thiết bị không được để trống")]
            [DataType(DataType.Date, ErrorMessage = "Ngày mua không đúng định dạng ngày tháng")]
            public DateTime PurchaseDate { get; set; }

            [Required(ErrorMessage = "Mã khách hàng sở hữu (CustomerId) không được để trống")]
            [Range(1, int.MaxValue, ErrorMessage = "CustomerId phải là số nguyên dương hợp lệ")]
            public int CustomerId { get; set; }
        }

        // =========================================================================
        // 2. DTO CẬP NHẬT THÔNG TIN THIẾT BỊ (Bắt buộc)
        // =========================================================================
        public class ProductUpdateDto
        {
            [Required(ErrorMessage = "Mã Serial/IMEI không được để trống")]
            [MaxLength(100, ErrorMessage = "Mã Serial/IMEI tối đa 100 ký tự")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Mã Serial/IMEI chỉ được chứa chữ cái, chữ số, dấu gạch ngang và gạch dưới")]
            public string SerialNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "Tên thiết bị không được để trống")]
            [MaxLength(150, ErrorMessage = "Tên thiết bị tối đa 150 ký tự")]
            public string Name { get; set; } = string.Empty;

            [MaxLength(100, ErrorMessage = "Model/Dòng máy tối đa 100 ký tự")]
            public string Model { get; set; } = string.Empty;

            [MaxLength(100, ErrorMessage = "Thương hiệu tối đa 100 ký tự")]
            public string Brand { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ngày mua thiết bị không được để trống")]
            [DataType(DataType.Date, ErrorMessage = "Ngày mua không đúng định dạng ngày tháng")]
            public DateTime PurchaseDate { get; set; }

            [Required(ErrorMessage = "Mã khách hàng sở hữu (CustomerId) không được để trống")]
            [Range(1, int.MaxValue, ErrorMessage = "CustomerId phải là số nguyên dương hợp lệ")]
            public int CustomerId { get; set; }
        }

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
}