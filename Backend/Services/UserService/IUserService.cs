using EWarrantySystem.DTOs;

namespace EWarrantySystem.Services
{
    public interface IUserService
    {
        Task<(bool Success, string? ErrorMessage, UserResponseDto? Data)> RegisterAsync(
            UserRegisterDto request, 
            bool isAdmin = false);

        Task<(bool Success, string? ErrorMessage, string? Token, string? RefreshToken, UserResponseDto? Data)> LoginAsync(
            UserLoginDto request);

        Task<(bool Success, string? ErrorMessage, string? AccessToken, string? RefreshToken, UserResponseDto? Data)> RefreshTokenAsync(
            string refreshToken);

        Task<(bool Success, string? ErrorMessage, UserResponseDto? Data)> UpdateProfileAsync(
            int id, 
            UserUpdateProfileDto request);

        Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(
            int id, 
            ChangePasswordDto request);

        Task<(bool Success, string? ErrorMessage, UserResponseDto? Data)> UpdateRoleAndStatusAsync(
            UserUpdateRoleDto request);

        Task<(bool Success, string? ErrorMessage, UserResponseDto? Data)> SoftDeleteAsync(int id);
    }
}