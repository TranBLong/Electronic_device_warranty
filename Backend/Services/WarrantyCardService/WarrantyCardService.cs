using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Data;
using EWarrantySystem.DTOs;
using EWarrantySystem.Models;

namespace EWarrantySystem.Services
{
    public class WarrantyCardService : IWarrantyCardService
    {
        private readonly AppDbContext _context;

        public WarrantyCardService(AppDbContext context)
        {
            _context = context;
        }

        // ==================== CREATE ====================
        public async Task<(bool Success, string? ErrorMessage, WarrantyCardResponseDto? Data)> CreateAsync(
            WarrantyCardCreateDto request)
        {
            // 1. Kiểm tra Product tồn tại
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
                return (false, $"Không tìm thấy thiết bị với ProductId = {request.ProductId}!", null);

            // 2. Kiểm tra thiết bị đã có thẻ bảo hành chưa (quan hệ 1-1)
            var existingCardForProduct = await _context.WarrantyCards
                .AnyAsync(w => w.ProductId == request.ProductId);

            if (existingCardForProduct)
                return (false, $"Thiết bị (ID: {request.ProductId}) đã được đính kèm một thẻ bảo hành khác!", null);

            // 3. Kiểm tra CardCode trùng
            var cardCodeExists = await _context.WarrantyCards
                .AnyAsync(w => w.CardCode.ToLower() == request.CardCode.Trim().ToLower());

            if (cardCodeExists)
                return (false, $"Mã thẻ bảo hành '{request.CardCode}' đã tồn tại trên hệ thống!", null);

            // 4. Kiểm tra logic ngày
            if (request.EndDate <= request.StartDate)
                return (false, "Ngày hết hạn bảo hành (EndDate) phải sau ngày bắt đầu (StartDate)!", null);

            // 5. Tạo entity
            var newCard = new WarrantyCard
            {
                CardCode = request.CardCode.Trim(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                WarrantyType = string.IsNullOrWhiteSpace(request.WarrantyType) 
                    ? "Standard" 
                    : request.WarrantyType.Trim(),
                IsActive = request.IsActive,
                ProductId = request.ProductId
            };

            _context.WarrantyCards.Add(newCard);
            await _context.SaveChangesAsync();

            // 6. Load navigation
            await _context.Entry(newCard).Reference(w => w.Product).LoadAsync();
            if (newCard.Product != null)
            {
                await _context.Entry(newCard.Product).Reference(p => p.Customer).LoadAsync();
            }

            return (true, null, MapToResponseDto(newCard));
        }

        // ==================== UPDATE ====================
        public async Task<(bool Success, string? ErrorMessage, WarrantyCardResponseDto? Data)> UpdateAsync(
            int id, 
            WarrantyCardUpdateDto request)
        {
            var card = await _context.WarrantyCards
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Customer)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (card == null)
                return (false, $"Không tìm thấy thẻ bảo hành có Id = {id}", null);

            // Kiểm tra EndDate hợp lệ so với StartDate hiện tại
            if (request.EndDate <= card.StartDate)
                return (false, "Ngày hết hạn mới phải sau ngày bắt đầu kích hoạt thẻ!", null);

            card.EndDate = request.EndDate;
            card.WarrantyType = request.WarrantyType.Trim();
            card.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return (true, null, MapToResponseDto(card));
        }

        // ==================== DELETE ====================
        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id)
        {
            var card = await _context.WarrantyCards.FindAsync(id);
            if (card == null)
                return (false, $"Không tìm thấy thẻ bảo hành có Id = {id}");

            _context.WarrantyCards.Remove(card);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        #region --- Hàm map DTO ---

        private static WarrantyCardResponseDto MapToResponseDto(WarrantyCard card)
        {
            var now = DateTime.UtcNow;

            bool isValid = card.IsActive && card.EndDate >= now;

            int daysRemaining = 0;
            if (card.EndDate > now)
                daysRemaining = (int)(card.EndDate - now).TotalDays;

            return new WarrantyCardResponseDto
            {
                Id = card.Id,
                CardCode = card.CardCode,
                StartDate = card.StartDate,
                EndDate = card.EndDate,
                WarrantyType = card.WarrantyType,
                IsActive = card.IsActive,

                ProductId = card.ProductId,
                ProductName = card.Product?.Name ?? string.Empty,
                ProductSerialNumber = card.Product?.SerialNumber ?? string.Empty,

                CustomerName = card.Product?.Customer?.FullName ?? string.Empty,

                IsValid = isValid,
                DaysRemaining = daysRemaining
            };
        }

        #endregion
    }
}