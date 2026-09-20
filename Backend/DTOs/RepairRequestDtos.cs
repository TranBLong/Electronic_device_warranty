using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// Lớp chứa tất cả các DTOs liên quan đến Phân hệ Quản lý Phiếu yêu cầu sửa chữa / bảo hành (RepairRequest)
    /// </summary>
    public class RepairRequestDtos
    {
        // =========================================================================
        // 1. DTO TẠO PHIẾU BẢO HÀNH (Khách hàng tự tạo online hoặc Lễ tân tạo tại quầy)
        // =========================================================================
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
            /// Mã lễ tân lập phiếu (Chỉ truyền vào khi Lễ tân lập phiếu giúp Khách tại cửa hàng)
            /// </summary>
            [Range(1, int.MaxValue, ErrorMessage = "ReceptionistId phải là số nguyên dương hợp lệ")]
            public int? ReceptionistId { get; set; }
        }

        // =========================================================================
        // 2. DTO GÁN KỸ THUẬT VIÊN PHỤ TRÁCH (Lễ tân / Manager thực hiện)
        // =========================================================================
        public class RepairRequestAssignDto
        {
            [Required(ErrorMessage = "Mã kỹ thuật viên (TechnicianId) không được để trống")]
            [Range(1, int.MaxValue, ErrorMessage = "TechnicianId phải là số nguyên dương hợp lệ")]
            public int TechnicianId { get; set; }
        }

        // =========================================================================
        // 3. DTO CẬP NHẬT TIẾN ĐỘ & GHI CHÚ (Kỹ thuật viên thực hiện)
        // =========================================================================
        public class RepairRequestUpdateStatusDto
        {
            [Required(ErrorMessage = "Trạng thái phiếu không được để trống")]
            [RegularExpression(@"^(Pending|InProgress|Completed|Returned|Cancelled)$", 
                ErrorMessage = "Trạng thái không hợp lệ (Chỉ chấp nhận: Pending, InProgress, Completed, Returned, Cancelled)")]
            public string Status { get; set; } = string.Empty;

            [MaxLength(500, ErrorMessage = "Ghi chú đánh giá kỹ thuật tối đa 500 ký tự")]
            public string? TechnicalNote { get; set; }

            [DataType(DataType.Date, ErrorMessage = "Ngày hẹn trả máy không đúng định dạng")]
            public DateTime? EstimatedReturnDate { get; set; }
        }

        // =========================================================================
        // 4. DTO TRẢ DỮ LIỆU PHIẾU SỬA CHỮA CHO CLIENT
        // =========================================================================
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

            // Thông tin Thiết bị mang đến sửa
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string ProductSerialNumber { get; set; } = string.Empty;

            // Thông tin Khách hàng sở hữu
            public int CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerPhoneNumber { get; set; } = string.Empty;

            // Thông tin Lễ tân tiếp nhận phiếu (null nếu Khách tự gửi online)
            public int? ReceptionistId { get; set; }
            public string? ReceptionistName { get; set; }

            // Thông tin Kỹ thuật viên phụ trách sửa chữa (null nếu chưa được phân công)
            public int? TechnicianId { get; set; }
            public string? TechnicianName { get; set; }
        }
    }
}