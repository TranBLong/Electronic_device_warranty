using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Phân quyền hoặc khóa/mở khóa tài khoản (Admin/Manager thực hiện)[cite: 1, 2]
    /// </summary>
    public class UserUpdateRoleDto
    {
        [Required(ErrorMessage = "Mã người dùng (UserId) không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "UserId phải là số nguyên dương hợp lệ")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [RegularExpression("^(Admin|Manager|Receptionist|Technician|Customer)$", ErrorMessage = "Vai trò phải thuộc một trong các giá trị: Admin, Manager, Receptionist, Technician, Customer")]
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}