using EWarrantySystem.DTOs;

namespace EWarrantySystem.Services
{
    public interface IProductService
    {
        Task<(bool Success, string? ErrorMessage, ProductResponseDto? Data)> CreateAsync(ProductCreateDto request);
        // Có thể thêm GetAll, Update, Delete sau nếu muốn
    }
}