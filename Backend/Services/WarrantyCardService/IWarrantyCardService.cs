using EWarrantySystem.DTOs;

namespace EWarrantySystem.Services
{
    public interface IWarrantyCardService
    {
        Task<(bool Success, string? ErrorMessage, WarrantyCardResponseDto? Data)> CreateAsync(
            WarrantyCardCreateDto request);

        Task<(bool Success, string? ErrorMessage, WarrantyCardResponseDto? Data)> UpdateAsync(
            int id, 
            WarrantyCardUpdateDto request);

        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id);
    }
}