using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Models;
using static EWarrantySystem.DTOs.WarrantyCardDtos;
using EWarrantySystem.Data; // <-- THÊM DÒNG NÀY

namespace EWarrantySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: /api/warrantycards
    public class WarrantyCardsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WarrantyCardsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API 1: LẤY DANH SÁCH TẤT CẢ THẺ BẢO HÀNH (Có hỗ trợ lọc theo ProductId hoặc trạng thái)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? productId, [FromQuery] bool? isActive, [FromQuery] string? searchCode)
        {
            var query = _context.WarrantyCards
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Customer)
                .AsQueryable();

            // Lọc theo mã thiết bị (ProductId)
            if (productId.HasValue && productId.Value > 0)
            {
                query = query.Where(w => w.ProductId == productId.Value);
            }

            // Lọc theo trạng thái hiệu lực (IsActive)
            if (isActive.HasValue)
            {
                query = query.Where(w => w.IsActive == isActive.Value);
            }

            // Tìm kiếm theo Mã thẻ bảo hành
            if (!string.IsNullOrWhiteSpace(searchCode))
            {
                var keyword = searchCode.Trim().ToLower();
                query = query.Where(w => w.CardCode.ToLower().Contains(keyword));
            }

            var cards = await query.ToListAsync();
            var responseList = cards.Select(MapToResponseDto).ToList();

            return Ok(responseList);
        }

        /// <summary>
        /// API 2: LẤY THÔNG TIN CHI TIẾT THẺ BẢO HÀNH THEO ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var card = await _context.WarrantyCards
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Customer)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (card == null)
            {
                return NotFound(new { message = $"Không tìm thấy thẻ bảo hành có Id = {id}" });
            }

            return Ok(MapToResponseDto(card));
        }

        /// <summary>
        /// API 3: TRA CỨU THẺ BẢO HÀNH THEO MÃ THẺ (CardCode)
        /// </summary>
        [HttpGet("by-code/{cardCode}")]
        public async Task<IActionResult> GetByCardCode(string cardCode)
        {
            var card = await _context.WarrantyCards
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Customer)
                .FirstOrDefaultAsync(w => w.CardCode.ToLower() == cardCode.Trim().ToLower());

            if (card == null)
            {
                return NotFound(new { message = $"Không tìm thấy thẻ bảo hành có mã '{cardCode}'" });
            }

            return Ok(MapToResponseDto(card));
        }

        /// <summary>
        /// API 4: TẠO MỚI / KÍCH HOẠT THẺ BẢO HÀNH (WarrantyCardCreateDto)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WarrantyCardCreateDto request)
        {
            // 1. Kiểm tra Thiết bị (Product) có tồn tại trong hệ thống không
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return BadRequest(new { message = $"Không tìm thấy thiết bị với ProductId = {request.ProductId}!" });
            }

            // 2. Kiểm tra xem Thiết bị đã có Thẻ bảo hành chưa (Quan hệ 1-1)
            var existingCardForProduct = await _context.WarrantyCards
                .AnyAsync(w => w.ProductId == request.ProductId);
            if (existingCardForProduct)
            {
                return BadRequest(new { message = $"Thiết bị (ID: {request.ProductId}) đã được đính kèm một thẻ bảo hành khác!" });
            }

            // 3. Kiểm tra Mã thẻ bảo hành (CardCode) có bị trùng lặp không
            var cardCodeExists = await _context.WarrantyCards
                .AnyAsync(w => w.CardCode.ToLower() == request.CardCode.Trim().ToLower());
            if (cardCodeExists)
            {
                return BadRequest(new { message = $"Mã thẻ bảo hành '{request.CardCode}' đã tồn tại trên hệ thống!" });
            }

            // 4. Kiểm tra logic Ngày bắt đầu và Ngày kết thúc
            if (request.EndDate <= request.StartDate)
            {
                return BadRequest(new { message = "Ngày hết hạn bảo hành (EndDate) phải sau ngày bắt đầu (StartDate)!" });
            }

            // 5. Khởi tạo thực thể WarrantyCard
            var newCard = new WarrantyCard
            {
                CardCode = request.CardCode.Trim(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                WarrantyType = string.IsNullOrWhiteSpace(request.WarrantyType) ? "Standard" : request.WarrantyType.Trim(),
                IsActive = request.IsActive,
                ProductId = request.ProductId
            };

            _context.WarrantyCards.Add(newCard);
            await _context.SaveChangesAsync();

            // 6. Load dữ liệu liên quan để map sang ResponseDto
            await _context.Entry(newCard)
                .Reference(w => w.Product)
                .LoadAsync();
            if (newCard.Product != null)
            {
                await _context.Entry(newCard.Product)
                    .Reference(p => p.Customer)
                    .LoadAsync();
            }

            var responseDto = MapToResponseDto(newCard);

            return CreatedAtAction(nameof(GetById), new { id = newCard.Id }, responseDto);
        }

        /// <summary>
        /// API 5: GIA HẠN / ĐỔI LOẠI / CẬP NHẬT TRẠNG THÁI THẺ (WarrantyCardUpdateDto)
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] WarrantyCardUpdateDto request)
        {
            var card = await _context.WarrantyCards
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Customer)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (card == null)
            {
                return NotFound(new { message = $"Không tìm thấy thẻ bảo hành có Id = {id}" });
            }

            // Kiểm tra Ngày hết hạn hợp lệ so với Ngày bắt đầu hiện tại
            if (request.EndDate <= card.StartDate)
            {
                return BadRequest(new { message = "Ngày hết hạn mới phải sau ngày bắt đầu kích hoạt thẻ!" });
            }

            // Cập nhật thông tin
            card.EndDate = request.EndDate;
            card.WarrantyType = request.WarrantyType.Trim();
            card.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật thông tin thẻ bảo hành thành công!",
                warrantyCard = MapToResponseDto(card)
            });
        }

        /// <summary>
        /// API 6: VÔ HIỆU HÓA / XÓA THẺ BẢO HÀNH
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var card = await _context.WarrantyCards.FindAsync(id);

            if (card == null)
            {
                return NotFound(new { message = $"Không tìm thấy thẻ bảo hành có Id = {id}" });
            }

            _context.WarrantyCards.Remove(card);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #region --- HÀM BỔ TRỢ CHUYỂN ĐỔI DTO ---

        /// <summary>
        /// Chuyển đổi từ Model WarrantyCard sang WarrantyCardResponseDto bao gồm IsValid và DaysRemaining
        /// </summary>
        private static WarrantyCardResponseDto MapToResponseDto(WarrantyCard card)
        {
            var now = DateTime.UtcNow;
            
            // Thẻ còn hiệu lực nếu IsActive = true và Ngày hiện tại <= EndDate
            bool isValid = card.IsActive && card.EndDate >= now;

            // Tính số ngày còn lại
            int daysRemaining = 0;
            if (card.EndDate > now)
            {
                daysRemaining = (int)(card.EndDate - now).TotalDays;
            }

            return new WarrantyCardResponseDto
            {
                Id = card.Id,
                CardCode = card.CardCode,
                StartDate = card.StartDate,
                EndDate = card.EndDate,
                WarrantyType = card.WarrantyType,
                IsActive = card.IsActive,

                // Thông tin Thiết bị
                ProductId = card.ProductId,
                ProductName = card.Product?.Name ?? string.Empty,
                ProductSerialNumber = card.Product?.SerialNumber ?? string.Empty,

                // Thông tin Khách hàng
                CustomerName = card.Product?.Customer?.FullName ?? string.Empty,

                // Trường tính toán hỗ trợ Client UI
                IsValid = isValid,
                DaysRemaining = daysRemaining
            };
        }

        #endregion
    }
}