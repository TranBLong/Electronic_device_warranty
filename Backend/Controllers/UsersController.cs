using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Models;
using EWarrantySystem.Data;
using EWarrantySystem.Services;
using EWarrantySystem.DTOs;

namespace EWarrantySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserService _userService;

        public UsersController(AppDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/users  → chỉ Admin / Manager
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? role,
            [FromQuery] bool? isActive,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role.ToLower() == role.ToLower());

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(keyword) ||
                                         u.FullName.ToLower().Contains(keyword) ||
                                         u.Email.ToLower().Contains(keyword) ||
                                         (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(keyword)));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responseList = users.Select(MapToResponseDto).ToList();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                data = responseList
            });
        }

        // GET: api/users/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            // Chỉ Admin/Manager hoặc chính chủ mới được xem thông tin.
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && currentUserId != id)
                return Forbid();

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
            bool isAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
            var result = await _userService.RegisterAsync(request, isAdmin);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
        }

        // POST: api/users/login  → Public, trả về JWT
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var result = await _userService.LoginAsync(request);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = "Đăng nhập thành công!",
                token = result.Token,
                refreshToken = result.RefreshToken,
                user = result.Data
            });
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto request)
        {
            var result = await _userService.RefreshTokenAsync(request.RefreshToken);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = "Làm mới token thành công!",
                token = result.AccessToken,
                refreshToken = result.RefreshToken,
                user = result.Data
            });
        }

        // POST: api/users/logout
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto request)
        {
            var result = await _userService.LogoutAsync(request.RefreshToken);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Đăng xuất thành công!" });
        }

        // PUT: api/users/{id}/profile
        [HttpPut("{id:int}/profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UserUpdateProfileDto request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            // Chỉ Admin/Manager hoặc chính chủ mới được cập nhật hồ sơ.
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && currentUserId != id)
                return Forbid();

            var result = await _userService.UpdateProfileAsync(id, request);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Cập nhật thông tin thành công!", user = result.Data });
        }

        // PUT: api/users/role-status  → chỉ Admin / Manager
        [HttpPut("role-status")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRoleAndStatus([FromBody] UserUpdateRoleDto request)
        {
            var result = await _userService.UpdateRoleAndStatusAsync(request);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = "Cập nhật vai trò/trạng thái tài khoản thành công!",
                user = result.Data
            });
        }

        // PUT: api/users/{id}/change-password
        [HttpPut("{id:int}/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            // Đổi mật khẩu chỉ dành cho chính chủ, kể cả Admin/Manager.
            if (currentUserId != id)
                return Forbid();

            var result = await _userService.ChangePasswordAsync(id, request);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        // DELETE: api/users/{id}  → chỉ Admin / Manager
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _userService.SoftDeleteAsync(id);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new
            {
                message = $"Đã khóa (xóa mềm) tài khoản người dùng Id = {id} thành công!",
                user = result.Data
            });
        }

        #region --- HÀM BỔ TRỢ ---
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
