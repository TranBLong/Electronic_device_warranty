// using System.ComponentModel.DataAnnotations;

// namespace EWarrantySystem.DTOs
// {
//     /// <summary>
//     /// Lớp chứa tất cả các DTOs liên quan đến Phân hệ Quản lý Tài khoản & Xác thực
//     /// </summary>
//     public class UserDtos
//     {
//         // =========================================================================
//         // 1. DTO ĐĂNG KÝ (Bắt buộc)
//         // =========================================================================
//         public class UserRegisterDto
//         {
//             [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
//             [MinLength(3, ErrorMessage = "Tên đăng nhập tối thiểu 3 ký tự")]
//             [MaxLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự")]
//             [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, chữ số và dấu gạch dưới")]
//             public string Username { get; set; } = string.Empty;

//             [Required(ErrorMessage = "Họ và tên không được để trống")]
//             [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự")]
//             public string FullName { get; set; } = string.Empty;

//             [Required(ErrorMessage = "Email không được để trống")]
//             [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
//             [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
//             public string Email { get; set; } = string.Empty;

//             [Required(ErrorMessage = "Số điện thoại không được để trống")]
//             [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
//             [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ (ví dụ: 0912345678)")]
//             public string PhoneNumber { get; set; } = string.Empty;

//             [Required(ErrorMessage = "Mật khẩu không được để trống")]
//             [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
//             [MaxLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự")]
//             public string Password { get; set; } = string.Empty;

//             /// <summary>
//             /// Vai trò của người dùng trong hệ thống: Admin, Manager, Receptionist, Technician, Customer.
//             /// Mặc định khi khách hàng tự đăng ký là "Customer".
//             /// </summary>
//             [Required(ErrorMessage = "Vai trò không được để trống")]
//             [RegularExpression("^(Admin|Manager|Receptionist|Technician|Customer)$", ErrorMessage = "Vai trò không hợp lệ (Admin, Manager, Receptionist, Technician, Customer)")]
//             public string Role { get; set; } = "Customer";
//         }

//         // =========================================================================
//         // 2. DTO ĐĂNG NHẬP (Bắt buộc)
//         // =========================================================================
//         public class UserLoginDto
//         {
//             [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
//             public string Username { get; set; } = string.Empty;

//             [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
//             public string Password { get; set; } = string.Empty;
//         }

//         // =========================================================================
//         // 3. DTO TRẢ DỮ LIỆU CỦA USER - ẨN PASSWORD (Bắt buộc)
//         // =========================================================================
//         public class UserResponseDto
//         {
//             public int Id { get; set; }
//             public string Username { get; set; } = string.Empty;
//             public string FullName { get; set; } = string.Empty;
//             public string Email { get; set; } = string.Empty;
//             public string PhoneNumber { get; set; } = string.Empty;
//             public string Role { get; set; } = string.Empty;
//             public bool IsActive { get; set; }
//             public DateTime CreatedAt { get; set; }
//             public DateTime? UpdatedAt { get; set; }
//         }

//         // =========================================================================
//         // 4. DTO PHÂN QUYỀN / KHÓA TÀI KHOẢN (Admin/Manager sử dụng) (Bắt buộc)
//         // =========================================================================
//         public class UserUpdateRoleDto
//         {
//             [Required(ErrorMessage = "Mã người dùng (UserId) không được để trống")]
//             [Range(1, int.MaxValue, ErrorMessage = "UserId phải là số nguyên dương hợp lệ")]
//             public int UserId { get; set; }

//             [Required(ErrorMessage = "Vui lòng chọn vai trò")]
//             [RegularExpression("^(Admin|Manager|Receptionist|Technician|Customer)$", ErrorMessage = "Vai trò phải thuộc một trong các giá trị: Admin, Manager, Receptionist, Technician, Customer")]
//             public string Role { get; set; } = string.Empty;

//             public bool IsActive { get; set; } = true;
//         }

//         // =========================================================================
//         // 5. DTO ĐỔI MẬT KHẨU (Nên có)
//         // =========================================================================
//         public class ChangePasswordDto
//         {
//             [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
//             public string OldPassword { get; set; } = string.Empty;

//             [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
//             [MinLength(6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
//             [MaxLength(100, ErrorMessage = "Mật khẩu mới tối đa 100 ký tự")]
//             public string NewPassword { get; set; } = string.Empty;

//             [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
//             [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới")]
//             public string ConfirmNewPassword { get; set; } = string.Empty;
//         }

//         // =========================================================================
//         // 6. DTO SỬA THÔNG TIN CÁ NHÂN (PROFILE)
//         // =========================================================================
//         public class UserUpdateProfileDto
//         {
//             [Required(ErrorMessage = "Họ và tên không được để trống")]
//             [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự")]
//             public string FullName { get; set; } = string.Empty;

//             [Required(ErrorMessage = "Email không được để trống")]
//             [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
//             [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
//             public string Email { get; set; } = string.Empty;

//             [Required(ErrorMessage = "Số điện thoại không được để trống")]
//             [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
//             [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ")]
//             public string PhoneNumber { get; set; } = string.Empty;
//         }
//     }
// }