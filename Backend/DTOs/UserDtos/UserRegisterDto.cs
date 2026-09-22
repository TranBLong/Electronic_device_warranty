using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    /// <summary>
    /// DTO Đăng ký tài khoản người dùng[cite: 1, 2]
    /// </summary>
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MinLength(3, ErrorMessage = "Tên đăng nhập tối thiểu 3 ký tự")]
        [MaxLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, chữ số và dấu gạch dưới")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ (ví dụ: 0912345678)")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [MaxLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Vai trò của người dùng trong hệ thống: Admin, Manager, Receptionist, Technician, Customer.[cite: 1, 2]
        /// Mặc định khi khách hàng tự đăng ký là "Customer".[cite: 1, 2]
        /// </summary>
        [Required(ErrorMessage = "Vai trò không được để trống")]
        [RegularExpression("^(Admin|Manager|Receptionist|Technician|Customer)$", ErrorMessage = "Vai trò không hợp lệ (Admin, Manager, Receptionist, Technician, Customer)")]
        public string Role { get; set; } = "Customer";
    }
}