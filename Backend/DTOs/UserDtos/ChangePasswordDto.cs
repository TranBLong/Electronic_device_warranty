using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Đổi mật khẩu người dùng[cite: 1, 2]
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [MaxLength(100, ErrorMessage = "Mật khẩu mới tối đa 100 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}