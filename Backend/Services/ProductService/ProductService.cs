using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Data;
using EWarrantySystem.DTOs;
using EWarrantySystem.Models;

namespace EWarrantySystem.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string? ErrorMessage, ProductResponseDto? Data)> CreateAsync(ProductCreateDto request)
        {
            // 1. Check khách hàng tồn tại
            var customerExists = await _context.Users.AnyAsync(u => u.Id == request.CustomerId);
            if (!customerExists)
                return (false, $"Khách hàng sở hữu với CustomerId = {request.CustomerId} không tồn tại!", null);

            // 2. Check Serial trùng
            var serialExists = await _context.Products
                .AnyAsync(p => p.SerialNumber.ToLower() == request.SerialNumber.Trim().ToLower());
            if (serialExists)
                return (false, $"Mã Serial/IMEI '{request.SerialNumber}' đã tồn tại trên hệ thống!", null);

            // 3. Tạo entity
            var newProduct = new Product
            {
                SerialNumber = request.SerialNumber.Trim(),
                Name = request.Name.Trim(),
                Model = request.Model?.Trim() ?? string.Empty,
                Brand = request.Brand?.Trim() ?? string.Empty,
                PurchaseDate = request.PurchaseDate,
                CustomerId = request.CustomerId
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            await _context.Entry(newProduct).Reference(p => p.Customer).LoadAsync();

            var dto = MapToResponseDto(newProduct);
            return (true, null, dto);
        }

        private static ProductResponseDto MapToResponseDto(Product product)
        {
            bool isUnderWarranty = product.WarrantyCard != null
                && product.WarrantyCard.IsActive
                && product.WarrantyCard.EndDate >= DateTime.UtcNow;

            return new ProductResponseDto
            {
                Id = product.Id,
                SerialNumber = product.SerialNumber,
                Name = product.Name,
                Model = product.Model,
                Brand = product.Brand,
                PurchaseDate = product.PurchaseDate,
                CustomerId = product.CustomerId,
                CustomerName = product.Customer?.FullName ?? string.Empty,
                CustomerPhoneNumber = product.Customer?.PhoneNumber ?? string.Empty,
                WarrantyCardId = product.WarrantyCard?.Id,
                WarrantyCardCode = product.WarrantyCard?.CardCode,
                WarrantyEndDate = product.WarrantyCard?.EndDate,
                IsUnderWarranty = isUnderWarranty
            };
        }
    }
}