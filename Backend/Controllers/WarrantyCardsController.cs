using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Models;
using EWarrantySystem.Data;
using Microsoft.AspNetCore.Authorization;
using EWarrantySystem.DTOs;
using EWarrantySystem.Services;

namespace EWarrantySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: /api/warrantycards
    [Authorize]
    public class WarrantyCardsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWarrantyCardService _warrantyCardService;

        public WarrantyCardsController(AppDbContext context, IWarrantyCardService warrantyCardService)
        {
            _context = context;
            _warrantyCardService = warrantyCardService;
        }

        /// <summary>
        /// API 1: LẤY DANH SÁCH TẤT CẢ THẺ BẢO HÀNH (Có hỗ trợ lọc theo ProductId hoặc trạng thái)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? productId,
            [FromQuery] bool? isActive,
            [FromQuery] string? searchCode,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

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
            var searchKey = !string.IsNullOrWhiteSpace(search) ? search : searchCode;
            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                var keyword = searchKey.Trim().ToLower();
                query = query.Where(w => w.CardCode.ToLower().Contains(keyword));
            }

            var totalCount = await query.CountAsync();

            var cards = await query
                .OrderByDescending(w => w.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responseList = cards.Select(MapToResponseDto).ToList();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                data = responseList
            });
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
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> Create([FromBody] WarrantyCardCreateDto request)
        {
            var result = await _warrantyCardService.CreateAsync(request);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
        }

        /// <summary>
        /// API 5: GIA HẠN / ĐỔI LOẠI / CẬP NHẬT TRẠNG THÁI THẺ (WarrantyCardUpdateDto)
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> Update(int id, [FromBody] WarrantyCardUpdateDto request)
        {
            var result = await _warrantyCardService.UpdateAsync(id, request);

            if (!result.Success)
            {
                if (result.ErrorMessage!.Contains("Không tìm thấy thẻ"))
                    return NotFound(new { message = result.ErrorMessage });

                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new
            {
                message = "Cập nhật thông tin thẻ bảo hành thành công!",
                warrantyCard = result.Data
            });
        }

        /// <summary>
        /// API 6: VÔ HIỆU HÓA / XÓA THẺ BẢO HÀNH
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _warrantyCardService.DeleteAsync(id);

            if (!result.Success)
            {
                if (result.ErrorMessage!.Contains("Không tìm thấy"))
                    return NotFound(new { message = result.ErrorMessage });

                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { message = $"Vô hiệu hóa thẻ bảo hành có Id = {id} thành công!" });
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