using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using EWarrantySystem.Data;
using EWarrantySystem.DTOs;
using EWarrantySystem.Models;

namespace EWarrantySystem.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;

        public UserService(AppDbContext context, JwtTokenService jwtTokenService, IConfiguration configuration)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
        }

        // ==================== REGISTER ====================
        public async Task<(bool Success, string? ErrorMessage, UserResponseDto? Data)> RegisterAsync(
            UserRegisterDto request, 
            bool isAdmin = false)
        {
            // 1. Check username trùng
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
                return (false, "Tên đăng nhập đã tồn tại!", null);

            // 2. Xác định Role
            var roleToAssign = "Customer";
            if (isAdmin && !string.IsNullOrEmpty(request.Role))
                roleToAssign = request.Role;

            // 3. Hash password bằng BCrypt
            var newUser = new User
            {
                Username = request.Username,
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = Encoding.UTF8.GetBytes(HashPassword(request.Password)),
                Role = roleToAssign,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return (true, null, MapToResponseDto(newUser));
        }

        // ==================== LOGIN ====================
        public async Task<(bool Success, string? ErrorMessage, string? Token, string? RefreshToken, UserResponseDto? Data)> LoginAsync(
            UserLoginDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

            if (user == null)
                return (false, "Tên đăng nhập không tồn tại!", null, null, null);

            if (!user.IsActive)
                return (false, "Tài khoản hiện đang bị khóa!", null, null, null);

            var storedHash = Encoding.UTF8.GetString(user.PasswordHash);
            if (!VerifyPassword(request.Password, storedHash))
                return (false, "Mật khẩu không chính xác!", null, null, null);

            var token = _jwtTokenService.GenerateToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshDays = int.Parse(_configuration["Jwt:RefreshTokenExpireDays"] ?? "7");

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
            });
            await _context.SaveChangesAsync();

            return (true, null, token, refreshToken, MapToResponseDto(user));
        }

        // ==================== REFRESH TOKEN ====================
        public async Task<(bool Success, string? ErrorMessage, string? AccessToken, string? RefreshToken, UserResponseDto? Data)> RefreshTokenAsync(string refreshToken)
        {
            var stored = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (stored == null || !stored.IsActive || stored.User == null || !stored.User.IsActive)
                return (false, "Refresh token không hợp lệ hoặc đã hết hạn!", null, null, null);

            // Rotate: revoke cũ, cấp mới
            stored.RevokedAt = DateTime.UtcNow;

            var newAccess = _jwtTokenService.GenerateToken(stored.User);
            var newRefresh = _jwtTokenService.GenerateRefreshToken();
            var refreshDays = int.Parse(_configuration["Jwt:RefreshTokenExpireDays"] ?? "7");

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = stored.UserId,
                Token = newRefresh,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
            });
            await _context.SaveChangesAsync();

            return (true, null, newAccess, newRefresh, MapToResponseDto(stored.User));
        }

        // ==================== UPDATE PROFILE ====================
        public async Task<(bool Success, string? ErrorMessage, UserResponseDto? Data)> UpdateProfileAsync(
            int id, 
            UserUpdateProfileDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return (false, $"Không tìm thấy người dùng có Id = {id}", null);

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, null, MapToResponseDto(user));
        }

        // ==================== CHANGE PASSWORD ====================
        public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(
            int id, 
            ChangePasswordDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return (false, $"Không tìm thấy người dùng có Id = {id}");

            var storedHash = Encoding.UTF8.GetString(user.PasswordHash);
            if (!VerifyPassword(request.OldPassword, storedHash))
                return (false, "Mật khẩu cũ không chính xác!");

            user.PasswordHash = Encoding.UTF8.GetBytes(HashPassword(request.NewPassword));
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        // ==================== UPDATE ROLE / STATUS ====================
        public async Task<(bool Success, string? ErrorMessage, UserResponseDto? Data)> UpdateRoleAndStatusAsync(
            UserUpdateRoleDto request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return (false, $"Không tìm thấy người dùng có Id = {request.UserId}", null);

            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, null, MapToResponseDto(user));
        }

        // ==================== SOFT DELETE ====================
        public async Task<(bool Success, string? ErrorMessage, UserResponseDto? Data)> SoftDeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return (false, $"Không tìm thấy người dùng có Id = {id}", null);

            if (!user.IsActive)
                return (false, $"Tài khoản có Id = {id} đã bị khóa trước đó!", null);

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, null, MapToResponseDto(user));
        }

        #region --- Hàm phụ trợ ---

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private static bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
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
