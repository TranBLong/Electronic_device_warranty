using System.ComponentModel.DataAnnotations;

namespace EWarrantySystem.DTOs
{
    public class UserDtos
    {
        // 1. DTO cho Đăng ký / Tạo người dùng mới
        public class UserRegisterDto
        {
            [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
            [MinLength(3, ErrorMessage = "Tên đăng nhập tối thiểu 3 ký tự")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Họ và tên không được để trống")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email không được để trống")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
            public string Email { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu không được để trống")]
            [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
            public string Password { get; set; } = string.Empty;

            /// <summary>
            /// Vai trò: "Admin", "Manager", "Receptionist", "Technician", "Customer"
            /// Mặc định khi khách hàng tự đăng ký là "Customer"
            /// </summary>
            public string Role { get; set; } = "Customer";
        }

        // 2. DTO cho Đăng nhập
        public class UserLoginDto
        {
            [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            public string Password { get; set; } = string.Empty;
        }

        // 3. DTO cho Cập nhật thông tin cá nhân
        public class UserUpdateProfileDto
        {
            [Required(ErrorMessage = "Họ và tên không được để trống")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email không được để trống")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
            public string Email { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            public string PhoneNumber { get; set; } = string.Empty;
        }

        // 4. DTO cho Admin / Manager phân quyền hoặc khóa tài khoản
        public class UserUpdateRoleDto
        {
            [Required]
            public int UserId { get; set; }

            [Required(ErrorMessage = "Vui lòng chọn vai trò")]
            public string Role { get; set; } = string.Empty;

            public bool IsActive { get; set; } = true;
        }

        // 5. DTO cho Đổi mật khẩu
        public class ChangePasswordDto
        {
            [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
            public string OldPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
            [MinLength(6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
            public string NewPassword { get; set; } = string.Empty;
        }

        // 6. DTO Trả về thông tin Người dùng cho Client (Ẩn PasswordHash & PasswordSalt)
        public class UserResponseDto
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }
    }
}