using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using EWarrantySystem.Models;
using static EWarrantySystem.DTOs.UserDtos;
using EWarrantySystem.Data;

namespace EWarrantySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: /api/users
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API 1: LẤY DANH SÁCH TẤT CẢ NGƯỜI DÙNG (Có lọc theo Role / IsActive)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? role, [FromQuery] bool? isActive)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role.ToLower() == role.ToLower());
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var users = await query.ToListAsync();
            var response = users.Select(MapToResponseDto).ToList();

            return Ok(response);
        }

        /// <summary>
        /// API 2: LẤY THÔNG TIN 1 NGƯỜI DÙNG THEO ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {id}" });
            }

            return Ok(MapToResponseDto(user));
        }

        /// <summary>
        /// API 3: ĐĂNG KÝ / TẠO MỚI NGƯỜI DÙNG
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
            {
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });
            }

            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var newUser = new User
            {
                Username = request.Username,
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = string.IsNullOrEmpty(request.Role) ? "Customer" : request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var response = MapToResponseDto(newUser);
            return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, response);
        }

        /// <summary>
        /// API 4: ĐĂNG NHẬP
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (user == null)
            {
                return BadRequest(new { message = "Tên đăng nhập không tồn tại!" });
            }

            if (!user.IsActive)
            {
                return BadRequest(new { message = "Tài khoản hiện đang bị khóa!" });
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest(new { message = "Mật khẩu không chính xác!" });
            }

            var response = MapToResponseDto(user);
            return Ok(new
            {
                message = "Đăng nhập thành công!",
                user = response
            });
        }

        /// <summary>
        /// API 5: CẬP NHẬT THÔNG TIN CÁ NHÂN (PROFILE)
        /// </summary>
        [HttpPut("{id:int}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UserUpdateProfileDto request)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {id}" });
            }

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật thông tin thành công!",
                user = MapToResponseDto(user)
            });
        }

        /// <summary>
        /// API 6: PHÂN QUYỀN / ĐỔI TRẠNG THÁI TÀI KHOẢN (Admin / Manager)
        /// </summary>
        [HttpPut("role-status")]
        public async Task<IActionResult> UpdateRoleAndStatus([FromBody] UserUpdateRoleDto request)
        {
            var user = await _context.Users.FindAsync(request.UserId);

            if (user == null)
            {
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {request.UserId}" });
            }

            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật vai trò/trạng thái tài khoản thành công!",
                user = MapToResponseDto(user)
            });
        }

        /// <summary>
        /// API 7: ĐỔI MẬT KHẨU
        /// </summary>
        [HttpPut("{id:int}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto request)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {id}" });
            }

            if (!VerifyPasswordHash(request.OldPassword, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest(new { message = "Mật khẩu cũ không chính xác!" });
            }

            CreatePasswordHash(request.NewPassword, out byte[] newHash, out byte[] newSalt);
            user.PasswordHash = newHash;
            user.PasswordSalt = newSalt;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        /// <summary>
        /// API 8: XÓA MỀM / KHÓA TÀI KHOẢN NGƯỜI DÙNG THEO ID
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {id}" });
            }

            if (!user.IsActive)
            {
                return BadRequest(new { message = $"Tài khoản có Id = {id} đã bị khóa trước đó!" });
            }

            // Xóa mềm: Khóa tài khoản thay vì xóa hẳn bản ghi khỏi CSDL
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã khóa (xóa mềm) tài khoản người dùng Id = {id} thành công!",
                user = MapToResponseDto(user)
            });
        }

        #region --- HÀM BỔ TRỢ BẢO MẬT & CHUYỂN ĐỔI DTO ---

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(storedHash);
        }

        private static UserResponseDto MapToResponseDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        #endregion
    }
}