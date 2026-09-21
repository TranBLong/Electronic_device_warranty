using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Models;
using static EWarrantySystem.DTOs.ProductDtos;
using EWarrantySystem.Data; // <-- THÊM DÒNG NÀY

namespace EWarrantySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: /api/products
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API 1: LẤY DANH SÁCH TẤT CẢ THIẾT BỊ (Có hỗ trợ lọc theo CustomerId hoặc Tìm kiếm)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? customerId, [FromQuery] string? search)
        {
            var query = _context.Products
                .Include(p => p.Customer)
                .Include(p => p.WarrantyCard)
                .AsQueryable();

            // Lọc theo mã khách hàng nếu có
            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(p => p.CustomerId == customerId.Value);
            }

            // Tìm kiếm theo tên hoặc mã Serial/IMEI
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(keyword) || 
                                         p.SerialNumber.ToLower().Contains(keyword) ||
                                         p.Model.ToLower().Contains(keyword));
            }

            var products = await query.ToListAsync();
            var responseList = products.Select(MapToResponseDto).ToList();

            return Ok(responseList);
        }

        /// <summary>
        /// API 2: LẤY THÔNG TIN CHI TIẾT THIẾT BỊ THEO ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Customer)
                .Include(p => p.WarrantyCard)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { message = $"Không tìm thấy thiết bị có Id = {id}" });
            }

            return Ok(MapToResponseDto(product));
        }

        /// <summary>
        /// API 3: LẤY THÔNG TIN THIẾT BỊ THEO MÃ SERIAL / IMEI
        /// </summary>
        [HttpGet("by-serial/{serialNumber}")]
        public async Task<IActionResult> GetBySerialNumber(string serialNumber)
        {
            var product = await _context.Products
                .Include(p => p.Customer)
                .Include(p => p.WarrantyCard)
                .FirstOrDefaultAsync(p => p.SerialNumber.ToLower() == serialNumber.Trim().ToLower());

            if (product == null)
            {
                return NotFound(new { message = $"Không tìm thấy thiết bị với mã Serial/IMEI: {serialNumber}" });
            }

            return Ok(MapToResponseDto(product));
        }

        /// <summary>
        /// API 4: TẠO MỚI THIẾT BỊ (ProductCreateDto)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto request)
        {
            // 1. Kiểm tra Khách hàng sở hữu có tồn tại trong hệ thống không
            var customerExists = await _context.Users.AnyAsync(u => u.Id == request.CustomerId);
            if (!customerExists)
            {
                return BadRequest(new { message = $"Khách hàng sở hữu với CustomerId = {request.CustomerId} không tồn tại!" });
            }

            // 2. Kiểm tra mã Serial/IMEI có bị trùng lặp không
            var serialExists = await _context.Products.AnyAsync(p => p.SerialNumber.ToLower() == request.SerialNumber.Trim().ToLower());
            if (serialExists)
            {
                return BadRequest(new { message = $"Mã Serial/IMEI '{request.SerialNumber}' đã tồn tại trên hệ thống!" });
            }

            // 3. Khởi tạo thực thể Product mới
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

            // 4. Load lại dữ liệu bao gồm Customer và WarrantyCard để map DTO phản hồi
            await _context.Entry(newProduct).Reference(p => p.Customer).LoadAsync();

            var responseDto = MapToResponseDto(newProduct);

            return CreatedAtAction(nameof(GetById), new { id = newProduct.Id }, responseDto);
        }

        /// <summary>
        /// API 5: CẬP NHẬT THÔNG TIN THIẾT BỊ (ProductUpdateDto)
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateDto request)
        {
            var product = await _context.Products
                .Include(p => p.Customer)
                .Include(p => p.WarrantyCard)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { message = $"Không tìm thấy thiết bị có Id = {id}" });
            }

            // 1. Kiểm tra CustomerId mới có hợp lệ không
            if (product.CustomerId != request.CustomerId)
            {
                var customerExists = await _context.Users.AnyAsync(u => u.Id == request.CustomerId);
                if (!customerExists)
                {
                    return BadRequest(new { message = $"Khách hàng sở hữu với CustomerId = {request.CustomerId} không tồn tại!" });
                }
            }

            // 2. Kiểm tra nếu mã Serial/IMEI bị đổi thì có đụng với thiết bị khác không
            if (!product.SerialNumber.Equals(request.SerialNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var serialExists = await _context.Products
                    .AnyAsync(p => p.Id != id && p.SerialNumber.ToLower() == request.SerialNumber.Trim().ToLower());

                if (serialExists)
                {
                    return BadRequest(new { message = $"Mã Serial/IMEI '{request.SerialNumber}' đã được dùng cho thiết bị khác!" });
                }
            }

            // 3. Cập nhật thông tin
            product.SerialNumber = request.SerialNumber.Trim();
            product.Name = request.Name.Trim();
            product.Model = request.Model?.Trim() ?? string.Empty;
            product.Brand = request.Brand?.Trim() ?? string.Empty;
            product.PurchaseDate = request.PurchaseDate;
            product.CustomerId = request.CustomerId;

            await _context.SaveChangesAsync();

            // Load lại Customer mới nếu đã thay đổi CustomerId
            await _context.Entry(product).Reference(p => p.Customer).LoadAsync();

            return Ok(new
            {
                message = "Cập nhật thông tin thiết bị thành công!",
                product = MapToResponseDto(product)
            });
        }

        /// <summary>
        /// API 6: XÓA THIẾT BỊ
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { message = $"Không tìm thấy thiết bị có Id = {id}" });
            }

            // Kiểm tra xem thiết bị có đang gắn phiếu sửa chữa hay thẻ bảo hành không
            var hasRepairRequests = await _context.RepairRequests.AnyAsync(r => r.ProductId == id);
            if (hasRepairRequests)
            {
                return BadRequest(new { message = "Không thể xóa thiết bị đang có lịch sử phiếu yêu cầu sửa chữa!" });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent(); // Trả về 204 No Content chuẩn REST
        }

        #region --- HÀM BỔ TRỢ CHUYỂN ĐỔI DTO ---

        /// <summary>
        /// Chuyển đổi từ Model Product sang ProductResponseDto bao gồm kiểm tra hiệu lực bảo hành
        /// </summary>
        private static ProductResponseDto MapToResponseDto(Product product)
        {
            // Kiểm tra xem thiết bị có thẻ bảo hành active và chưa hết hạn không
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
                
                // Thông tin Khách hàng sở hữu
                CustomerId = product.CustomerId,
                CustomerName = product.Customer?.FullName ?? string.Empty,
                CustomerPhoneNumber = product.Customer?.PhoneNumber ?? string.Empty,

                // Thông tin Thẻ bảo hành đính kèm (nếu có)
                WarrantyCardId = product.WarrantyCard?.Id,
                WarrantyCardCode = product.WarrantyCard?.CardCode,
                WarrantyEndDate = product.WarrantyCard?.EndDate,
                IsUnderWarranty = isUnderWarranty
            };
        }

        #endregion
    }
}