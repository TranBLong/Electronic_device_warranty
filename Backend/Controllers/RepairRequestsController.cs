using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Models;
using static EWarrantySystem.DTOs.RepairRequestDtos;
using EWarrantySystem.Data;

namespace EWarrantySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: /api/repairrequests
    public class RepairRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RepairRequestsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API 1: LẤY DANH SÁCH TẤT CẢ PHIẾU SỬA CHỮA (Có lọc theo CustomerId, TechnicianId, Status, Keyword)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? customerId, 
            [FromQuery] int? technicianId, 
            [FromQuery] string? status, 
            [FromQuery] string? search)
        {
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

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            var responseList = requests.Select(MapToResponseDto).ToList();

            return Ok(responseList);
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
            // 1. Kiểm tra Thiết bị (Product) có tồn tại trong hệ thống không
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return BadRequest(new { message = $"Không tìm thấy thiết bị có ProductId = {request.ProductId}!" });
            }

            // 2. Kiểm tra Khách hàng (Customer) có tồn tại không
            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer == null)
            {
                return BadRequest(new { message = $"Không tìm thấy khách hàng có CustomerId = {request.CustomerId}!" });
            }

            // 3. Nếu có ReceptionistId, kiểm tra nhân viên Lễ tân có tồn tại không
            if (request.ReceptionistId.HasValue)
            {
                var receptionistExists = await _context.Users.AnyAsync(u => u.Id == request.ReceptionistId.Value);
                if (!receptionistExists)
                {
                    return BadRequest(new { message = $"Không tìm thấy lễ tân có ReceptionistId = {request.ReceptionistId.Value}!" });
                }
            }

            // 4. Sinh tự động Mã phiếu (ví dụ: REQ-20260921-1234)
            string generatedCode = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            // 5. Khởi tạo thực thể RepairRequest
            var newRequest = new RepairRequest
            {
                RequestCode = generatedCode,
                IssueDescription = request.IssueDescription.Trim(),
                ProductId = request.ProductId,
                CustomerId = request.CustomerId,
                ReceptionistId = request.ReceptionistId,
                Status = RepairStatusEnum.Pending, // Trạng thái ban đầu là Pending (Chờ tiếp nhận)
                CreatedAt = DateTime.UtcNow
            };

            _context.RepairRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            // 6. Load lại các Navigation Properties để trả về DTO đầy đủ
            await _context.Entry(newRequest).Reference(r => r.Product).LoadAsync();
            await _context.Entry(newRequest).Reference(r => r.Customer).LoadAsync();
            if (newRequest.ReceptionistId.HasValue)
            {
                await _context.Entry(newRequest).Reference(r => r.Receptionist).LoadAsync();
            }

            var responseDto = MapToResponseDto(newRequest);

            return CreatedAtAction(nameof(GetById), new { id = newRequest.Id }, responseDto);
        }

        /// <summary>
        /// API 5: GÁN KĨ THUẬT VIÊN PHỤ TRÁCH (RepairRequestAssignDto)
        /// (Dành cho Lễ tân / Manager thực hiện)
        /// </summary>
        [HttpPut("{id:int}/assign")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> AssignTechnician(int id, [FromBody] RepairRequestAssignDto request)
        {
            var repairRequest = await _context.RepairRequests.FindAsync(id);
            if (repairRequest == null)
                return NotFound(new { message = $"Không tìm thấy phiếu sửa chữa có Id = {id}" });

            var technician = await _context.Users.FindAsync(request.TechnicianId);
            if (technician == null)
                return BadRequest(new { message = $"Không tìm thấy người dùng có TechnicianId = {request.TechnicianId}!" });

            // ===== CHỈ CHẤP NHẬN ROLE = Technician =====
            if (!technician.Role.Equals("Technician", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = $"Người dùng ID = {request.TechnicianId} không phải là Kỹ thuật viên!" });
            }

            repairRequest.TechnicianId = request.TechnicianId;

            if (repairRequest.Status == RepairStatusEnum.Pending)
                repairRequest.Status = RepairStatusEnum.InProgress;

            await _context.SaveChangesAsync();

            // Load lại navigation...
            await _context.Entry(repairRequest).Reference(r => r.Product).LoadAsync();
            await _context.Entry(repairRequest).Reference(r => r.Customer).LoadAsync();
            await _context.Entry(repairRequest).Reference(r => r.Technician).LoadAsync();
            if (repairRequest.ReceptionistId.HasValue)
                await _context.Entry(repairRequest).Reference(r => r.Receptionist).LoadAsync();

            return Ok(new
            {
                message = $"Đã phân công Kỹ thuật viên '{technician.FullName}' cho phiếu {repairRequest.RequestCode} thành công!",
                repairRequest = MapToResponseDto(repairRequest)
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
            var repairRequest = await _context.RepairRequests.FindAsync(id);
            if (repairRequest == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu sửa chữa có Id = {id}" });
            }

            // Chuyển đổi chuỗi Status sang RepairStatusEnum
            if (!Enum.TryParse<RepairStatusEnum>(request.Status, true, out var newStatus))
            {
                return BadRequest(new { message = $"Trạng thái '{request.Status}' không hợp lệ! (Chấp nhận: Pending, InProgress, Completed, Returned, Cancelled)" });
            }

            // Cập nhật thông tin
            repairRequest.Status = newStatus;

            if (!string.IsNullOrWhiteSpace(request.TechnicalNote))
            {
                repairRequest.TechnicalNote = request.TechnicalNote.Trim();
            }

            if (request.EstimatedReturnDate.HasValue)
            {
                repairRequest.EstimatedReturnDate = request.EstimatedReturnDate.Value;
            }

            // Nếu trạng thái đổi sang Completed hoặc Returned thì cập nhật CompletedAt
            if ((newStatus == RepairStatusEnum.Completed || newStatus == RepairStatusEnum.Returned) && !repairRequest.CompletedAt.HasValue)
            {
                repairRequest.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Load thông tin trả về
            await _context.Entry(repairRequest).Reference(r => r.Product).LoadAsync();
            await _context.Entry(repairRequest).Reference(r => r.Customer).LoadAsync();
            if (repairRequest.TechnicianId.HasValue)
            {
                await _context.Entry(repairRequest).Reference(r => r.Technician).LoadAsync();
            }
            if (repairRequest.ReceptionistId.HasValue)
            {
                await _context.Entry(repairRequest).Reference(r => r.Receptionist).LoadAsync();
            }

            return Ok(new
            {
                message = "Cập nhật tiến độ sửa chữa thành công!",
                repairRequest = MapToResponseDto(repairRequest)
            });
        }

        /// <summary>
        /// API 7: XÓA PHIẾU SỬA CHỮA
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var repairRequest = await _context.RepairRequests.FindAsync(id);
            if (repairRequest == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu sửa chữa có Id = {id}" });
            }

            _context.RepairRequests.Remove(repairRequest);
            await _context.SaveChangesAsync();

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