using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Gán kỹ thuật viên phụ trách (Lễ tân / Manager thực hiện)[cite: 1, 2]
    /// </summary>
    public class RepairRequestAssignDto
    {
        [Required(ErrorMessage = "Mã kỹ thuật viên (TechnicianId) không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "TechnicianId phải là số nguyên dương hợp lệ")]
        public int TechnicianId { get; set; }
    }
}