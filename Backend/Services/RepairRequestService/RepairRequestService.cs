using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EWarrantySystem.Data;
using EWarrantySystem.DTOs;
using EWarrantySystem.Models;

namespace EWarrantySystem.Services
{
    public class RepairRequestService : IRepairRequestService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RepairRequestService> _logger;

        public RepairRequestService(AppDbContext context, ILogger<RepairRequestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== CREATE ====================
        public async Task<(bool Success, string? ErrorMessage, RepairRequestResponseDto? Data)> CreateAsync(
            RepairRequestCreateDto request,
            string? evidenceImageUrl = null)
        {
            // 1. Kiểm tra Product tồn tại
            var product = await _context.Products
                .Include(p => p.WarrantyCard)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId);
            if (product == null)
                return (false, $"Không tìm thấy thiết bị có ProductId = {request.ProductId}!", null);

            // 2. Kiểm tra hạn bảo hành
            var warranty = product.WarrantyCard;
            bool isUnderWarranty = warranty != null
                && warranty.IsActive
                && warranty.EndDate >= DateTime.UtcNow;

            if (!isUnderWarranty)
            {
                return (false,
                    "Thiết bị đã hết hạn bảo hành hoặc chưa có thẻ bảo hành hợp lệ. Không thể tạo phiếu sửa chữa bảo hành!",
                    null);
            }

            // 3. Kiểm tra Customer tồn tại
            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer == null)
                return (false, $"Không tìm thấy khách hàng có CustomerId = {request.CustomerId}!", null);

            // 4. Kiểm tra Receptionist (nếu có)
            if (request.ReceptionistId.HasValue)
            {
                var receptionistExists = await _context.Users
                    .AnyAsync(u => u.Id == request.ReceptionistId.Value);

                if (!receptionistExists)
                    return (false, $"Không tìm thấy lễ tân có ReceptionistId = {request.ReceptionistId.Value}!", null);
            }

            // 5. Sinh mã phiếu + kiểm tra trùng
            string generatedCode;
            do
            {
                generatedCode = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            }
            while (await _context.RepairRequests.AnyAsync(r => r.RequestCode == generatedCode));

            // 6. Tạo entity
            var newRequest = new RepairRequest
            {
                RequestCode = generatedCode,
                IssueDescription = request.IssueDescription.Trim(),
                ProductId = request.ProductId,
                CustomerId = request.CustomerId,
                ReceptionistId = request.ReceptionistId,
                Status = RepairStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow,
                EvidenceImageUrl = evidenceImageUrl
            };

            _context.RepairRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            var code = newRequest.RequestCode;
            var customerId = newRequest.CustomerId;
            _logger.LogInformation("Tạo phiếu sửa chữa {RequestCode} cho CustomerId={CustomerId}", code, customerId);

            // Load navigation properties
            await _context.Entry(newRequest).Reference(r => r.Product).LoadAsync();
            await _context.Entry(newRequest).Reference(r => r.Customer).LoadAsync();
            if (newRequest.ReceptionistId.HasValue)
                await _context.Entry(newRequest).Reference(r => r.Receptionist).LoadAsync();

            return (true, null, MapToResponseDto(newRequest));
        }

        // ==================== ASSIGN TECHNICIAN ====================
        public async Task<(bool Success, string? ErrorMessage, RepairRequestResponseDto? Data)> AssignTechnicianAsync(
            int id, 
            RepairRequestAssignDto request,
            int changedByUserId)
        {
            var repairRequest = await _context.RepairRequests.FindAsync(id);
            if (repairRequest == null)
                return (false, $"Không tìm thấy phiếu sửa chữa có Id = {id}", null);

            var technician = await _context.Users.FindAsync(request.TechnicianId);
            if (technician == null)
                return (false, $"Không tìm thấy người dùng có TechnicianId = {request.TechnicianId}!", null);

            if (!technician.Role.Equals("Technician", StringComparison.OrdinalIgnoreCase))
                return (false, $"Người dùng ID = {request.TechnicianId} không phải là Kỹ thuật viên!", null);

            var oldStatus = repairRequest.Status.ToString();
            repairRequest.TechnicianId = request.TechnicianId;

            if (repairRequest.Status == RepairStatusEnum.Pending)
                repairRequest.Status = RepairStatusEnum.InProgress;

            // Lưu lịch sử thay đổi
            var history = new RepairRequestStatusHistory
            {
                RepairRequestId = repairRequest.Id,
                FromStatus = oldStatus,
                ToStatus = repairRequest.Status.ToString(),
                Note = $"Phân công kỹ thuật viên ID: {request.TechnicianId}",
                ChangedByUserId = changedByUserId,
                ChangedAt = DateTime.UtcNow
            };
            _context.RepairRequestStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            // Load lại navigation
            await _context.Entry(repairRequest).Reference(r => r.Product).LoadAsync();
            await _context.Entry(repairRequest).Reference(r => r.Customer).LoadAsync();
            await _context.Entry(repairRequest).Reference(r => r.Technician).LoadAsync();
            if (repairRequest.ReceptionistId.HasValue)
                await _context.Entry(repairRequest).Reference(r => r.Receptionist).LoadAsync();

            return (true, null, MapToResponseDto(repairRequest));
        }

        // ==================== UPDATE STATUS ====================
        public async Task<(bool Success, string? ErrorMessage, RepairRequestResponseDto? Data)> UpdateStatusAsync(
            int id, 
            RepairRequestUpdateStatusDto request,
            int changedByUserId)
        {
            var repairRequest = await _context.RepairRequests.FindAsync(id);
            if (repairRequest == null)
                return (false, $"Không tìm thấy phiếu sửa chữa có Id = {id}", null);

            if (!Enum.TryParse<RepairStatusEnum>(request.Status, true, out var newStatus))
            {
                return (false, 
                    $"Trạng thái '{request.Status}' không hợp lệ! (Chấp nhận: Pending, InProgress, Completed, Returned, Cancelled)", 
                    null);
            }

            var oldStatus = repairRequest.Status.ToString();
            repairRequest.Status = newStatus;

            if (!string.IsNullOrWhiteSpace(request.TechnicalNote))
                repairRequest.TechnicalNote = request.TechnicalNote.Trim();

            if (request.EstimatedReturnDate.HasValue)
                repairRequest.EstimatedReturnDate = request.EstimatedReturnDate.Value;

            if ((newStatus == RepairStatusEnum.Completed || newStatus == RepairStatusEnum.Returned) 
                && !repairRequest.CompletedAt.HasValue)
            {
                repairRequest.CompletedAt = DateTime.UtcNow;
            }

            // Lưu lịch sử thay đổi trạng thái
            var history = new RepairRequestStatusHistory
            {
                RepairRequestId = repairRequest.Id,
                FromStatus = oldStatus,
                ToStatus = newStatus.ToString(),
                Note = request.TechnicalNote?.Trim(),
                ChangedByUserId = changedByUserId,
                ChangedAt = DateTime.UtcNow
            };
            _context.RepairRequestStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            // Load navigation
            await _context.Entry(repairRequest).Reference(r => r.Product).LoadAsync();
            await _context.Entry(repairRequest).Reference(r => r.Customer).LoadAsync();
            if (repairRequest.TechnicianId.HasValue)
                await _context.Entry(repairRequest).Reference(r => r.Technician).LoadAsync();
            if (repairRequest.ReceptionistId.HasValue)
                await _context.Entry(repairRequest).Reference(r => r.Receptionist).LoadAsync();

            return (true, null, MapToResponseDto(repairRequest));
        }

        // ==================== DELETE ====================
        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id)
        {
            var repairRequest = await _context.RepairRequests.FindAsync(id);
            if (repairRequest == null)
                return (false, $"Không tìm thấy phiếu sửa chữa có Id = {id}");

            _context.RepairRequests.Remove(repairRequest);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        #region --- Hàm map DTO ---

        private static RepairRequestResponseDto MapToResponseDto(RepairRequest request)
        {
            return new RepairRequestResponseDto
            {
                Id = request.Id,
                RequestCode = request.RequestCode,
                IssueDescription = request.IssueDescription,
                TechnicalNote = request.TechnicalNote,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt,
                EstimatedReturnDate = request.EstimatedReturnDate,
                CompletedAt = request.CompletedAt,

                ProductId = request.ProductId,
                ProductName = request.Product?.Name ?? string.Empty,
                ProductSerialNumber = request.Product?.SerialNumber ?? string.Empty,

                CustomerId = request.CustomerId,
                CustomerName = request.Customer?.FullName ?? string.Empty,
                CustomerPhoneNumber = request.Customer?.PhoneNumber ?? string.Empty,

                ReceptionistId = request.ReceptionistId,
                ReceptionistName = request.Receptionist?.FullName,

                TechnicianId = request.TechnicianId,
                TechnicianName = request.Technician?.FullName,

                EvidenceImageUrl = request.EvidenceImageUrl
            };
        }

        #endregion
    }
}