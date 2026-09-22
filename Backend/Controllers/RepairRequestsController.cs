using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Models;
using EWarrantySystem.Data;
using EWarrantySystem.DTOs;
using EWarrantySystem.Services;

namespace EWarrantySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: /api/repairrequests
    public class RepairRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRepairRequestService _repairRequestService;

        public RepairRequestsController(AppDbContext context, IRepairRequestService repairRequestService)
        {
            _context = context;
            _repairRequestService = repairRequestService;
        }

        /// <summary>
        /// API 1: LẤY DANH SÁCH TẤT CẢ PHIẾU SỬA CHỮA (Có lọc theo CustomerId, TechnicianId, Status, Keyword)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? customerId, 
            [FromQuery] int? technicianId, 
            [FromQuery] string? status, 
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.RepairRequests
                .Include(r => r.Product)
                .Include(r => r.Customer)
                .Include(r => r.Receptionist)
                .Include(r => r.Technician)
                .AsQueryable();

            // Lọc theo Khách hàng
            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(r => r.CustomerId == customerId.Value);
            }

            // Lọc theo Kỹ thuật viên phụ trách
            if (technicianId.HasValue && technicianId.Value > 0)
            {
                query = query.Where(r => r.TechnicianId == technicianId.Value);
            }

            // Lọc theo Trạng thái (Pending, InProgress, Completed, Returned, Cancelled)
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RepairStatusEnum>(status, true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }

            // Tìm kiếm theo Mã phiếu hoặc Mô tả lỗi
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(r => r.RequestCode.ToLower().Contains(keyword) || 
                                         r.IssueDescription.ToLower().Contains(keyword));
            }

            var totalCount = await query.CountAsync();

            var requests = await query
                .OrderByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responseList = requests.Select(MapToResponseDto).ToList();

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
        /// API 2: LẤY THÔNG TIN CHI TIẾT PHIẾU SỬA CHỮA THEO ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _context.RepairRequests
                .Include(r => r.Product)
                .Include(r => r.Customer)
                .Include(r => r.Receptionist)
                .Include(r => r.Technician)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu sửa chữa có Id = {id}" });
            }

            return Ok(MapToResponseDto(request));
        }

        /// <summary>
        /// API 3: TRA CỨU PHIẾU SỬA CHỮA THEO MÃ PHIẾU (RequestCode)
        /// </summary>
        [HttpGet("by-code/{requestCode}")]
        public async Task<IActionResult> GetByCode(string requestCode)
        {
            var request = await _context.RepairRequests
                .Include(r => r.Product)
                .Include(r => r.Customer)
                .Include(r => r.Receptionist)
                .Include(r => r.Technician)
                .FirstOrDefaultAsync(r => r.RequestCode.ToLower() == requestCode.Trim().ToLower());

            if (request == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu sửa chữa có mã '{requestCode}'" });
            }

            return Ok(MapToResponseDto(request));
        }

        /// <summary>
        /// API 4: TẠO PHIẾU YÊU CẦU SỬA CHỮA (RepairRequestCreateDto)
        /// (Dành cho Khách hàng tạo online hoặc Lễ tân tạo tại quầy)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RepairRequestCreateDto request)
        {
            var result = await _repairRequestService.CreateAsync(request);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
        }

        /// <summary>
        /// API 5: GÁN KĨ THUẬT VIÊN PHỤ TRÁCH (RepairRequestAssignDto)
        /// (Dành cho Lễ tân / Manager thực hiện)
        /// </summary>
        [HttpPut("{id:int}/assign")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> AssignTechnician(int id, [FromBody] RepairRequestAssignDto request)
        {
            var result = await _repairRequestService.AssignTechnicianAsync(id, request);

            if (!result.Success)
            {
                if (result.ErrorMessage!.Contains("Không tìm thấy phiếu"))
                    return NotFound(new { message = result.ErrorMessage });

                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new
            {
                message = $"Đã phân công Kỹ thuật viên cho phiếu {result.Data!.RequestCode} thành công!",
                repairRequest = result.Data
            });
        }

        /// <summary>
        /// API 6: CẬP NHẬT TIẾN ĐỘ & GHI CHÚ KĨ THUẬT (RepairRequestUpdateStatusDto)
        /// (Dành cho Kỹ thuật viên thực hiện)
        /// </summary>
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin,Manager,Technician")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] RepairRequestUpdateStatusDto request)
        {
            var result = await _repairRequestService.UpdateStatusAsync(id, request);

            if (!result.Success)
            {
                if (result.ErrorMessage!.Contains("Không tìm thấy phiếu"))
                    return NotFound(new { message = result.ErrorMessage });

                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new
            {
                message = "Cập nhật tiến độ sửa chữa thành công!",
                repairRequest = result.Data
            });
        }

        /// <summary>
        /// API 7: XÓA PHIẾU SỬA CHỮA
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _repairRequestService.DeleteAsync(id);

            if (!result.Success)
                return NotFound(new { message = result.ErrorMessage });

            return NoContent();
        }

        #region --- HÀM BỔ TRỢ CHUYỂN ĐỔI DTO ---

        /// <summary>
        /// Chuyển đổi từ Model RepairRequest sang RepairRequestResponseDto
        /// </summary>
        private static RepairRequestResponseDto MapToResponseDto(RepairRequest request)
        {
            return new RepairRequestResponseDto
            {
                Id = request.Id,
                RequestCode = request.RequestCode,
                IssueDescription = request.IssueDescription,
                TechnicalNote = request.TechnicalNote,
                Status = request.Status.ToString(), // Chuyển Enum sang dạng string (Pending, InProgress, Completed, Returned, Cancelled)

                CreatedAt = request.CreatedAt,
                EstimatedReturnDate = request.EstimatedReturnDate,
                CompletedAt = request.CompletedAt,

                // Thông tin Thiết bị
                ProductId = request.ProductId,
                ProductName = request.Product?.Name ?? string.Empty,
                ProductSerialNumber = request.Product?.SerialNumber ?? string.Empty,

                // Thông tin Khách hàng
                CustomerId = request.CustomerId,
                CustomerName = request.Customer?.FullName ?? string.Empty,
                CustomerPhoneNumber = request.Customer?.PhoneNumber ?? string.Empty,

                // Thông tin Lễ tân
                ReceptionistId = request.ReceptionistId,
                ReceptionistName = request.Receptionist?.FullName,

                // Thông tin Kỹ thuật viên
                TechnicianId = request.TechnicianId,
                TechnicianName = request.Technician?.FullName
            };
        }

        #endregion
    }
}