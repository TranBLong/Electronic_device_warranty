using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using EWarrantySystem.Models;
using static EWarrantySystem.DTOs.UserDtos;
using EWarrantySystem.Data;
using EWarrantySystem.Services;

namespace EWarrantySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtTokenService _jwtTokenService;

        public UsersController(AppDbContext context, JwtTokenService jwtTokenService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
        }

        // GET: api/users  → chỉ Admin / Manager
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAll([FromQuery] string? role, [FromQuery] bool? isActive)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role.ToLower() == role.ToLower());

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            var users = await query.ToListAsync();
            var response = users.Select(MapToResponseDto).ToList();
            return Ok(response);
        }

        // GET: api/users/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {id}" });
            return Ok(MapToResponseDto(user));
        }

        // POST: api/users/register  → Public
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });

            // Chỉ cho phép đăng ký Role = Customer (trừ khi đã login Admin)
            var roleToAssign = "Customer";
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                roleToAssign = string.IsNullOrEmpty(request.Role) ? "Customer" : request.Role;
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
                Role = roleToAssign,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, MapToResponseDto(newUser));
        }

        // POST: api/users/login  → Public, trả về JWT
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (user == null)
                return BadRequest(new { message = "Tên đăng nhập không tồn tại!" });

            if (!user.IsActive)
                return BadRequest(new { message = "Tài khoản hiện đang bị khóa!" });

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
                return BadRequest(new { message = "Mật khẩu không chính xác!" });

            var token = _jwtTokenService.GenerateToken(user);

            return Ok(new
            {
                message = "Đăng nhập thành công!",
                token = token,
                user = MapToResponseDto(user)
            });
        }

        // PUT: api/users/{id}/profile
        [HttpPut("{id:int}/profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UserUpdateProfileDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {id}" });

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông tin thành công!", user = MapToResponseDto(user) });
        }

        // PUT: api/users/role-status  → chỉ Admin / Manager
        [HttpPut("role-status")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRoleAndStatus([FromBody] UserUpdateRoleDto request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {request.UserId}" });

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

        // PUT: api/users/{id}/change-password
        [HttpPut("{id:int}/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {id}" });

            if (!VerifyPasswordHash(request.OldPassword, user.PasswordHash, user.PasswordSalt))
                return BadRequest(new { message = "Mật khẩu cũ không chính xác!" });

            CreatePasswordHash(request.NewPassword, out byte[] newHash, out byte[] newSalt);
            user.PasswordHash = newHash;
            user.PasswordSalt = newSalt;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        // DELETE: api/users/{id}  → chỉ Admin / Manager
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = $"Không tìm thấy người dùng có Id = {id}" });

            if (!user.IsActive)
                return BadRequest(new { message = $"Tài khoản có Id = {id} đã bị khóa trước đó!" });

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã khóa (xóa mềm) tài khoản người dùng Id = {id} thành công!",
                user = MapToResponseDto(user)
            });
        }

        #region --- HÀM BỔ TRỢ ---
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