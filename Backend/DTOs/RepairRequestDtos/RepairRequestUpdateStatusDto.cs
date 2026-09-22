using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Cập nhật tiến độ & ghi chú (Kỹ thuật viên thực hiện)[cite: 1, 2]
    /// </summary>
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
}