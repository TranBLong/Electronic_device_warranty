using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Data;
using EWarrantySystem.DTOs;
using EWarrantySystem.Models;

namespace EWarrantySystem.Services
{
    public class RepairRequestService : IRepairRequestService
    {
        private readonly AppDbContext _context;

        public RepairRequestService(AppDbContext context)
        {
            _context = context;
        }

        // ==================== CREATE ====================
        public async Task<(bool Success, string? ErrorMessage, RepairRequestResponseDto? Data)> CreateAsync(
            RepairRequestCreateDto request)
        {
            // 1. Kiểm tra Product tồn tại
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
                return (false, $"Không tìm thấy thiết bị có ProductId = {request.ProductId}!", null);

            // 2. Kiểm tra Customer tồn tại
            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer == null)
                return (false, $"Không tìm thấy khách hàng có CustomerId = {request.CustomerId}!", null);

            // 3. Kiểm tra Receptionist (nếu có)
            if (request.ReceptionistId.HasValue)
            {
                var receptionistExists = await _context.Users
                    .AnyAsync(u => u.Id == request.ReceptionistId.Value);

                if (!receptionistExists)
                    return (false, $"Không tìm thấy lễ tân có ReceptionistId = {request.ReceptionistId.Value}!", null);
            }

            // 4. Sinh mã phiếu + kiểm tra trùng
            string generatedCode;
            do
            {
                generatedCode = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            }
            while (await _context.RepairRequests.AnyAsync(r => r.RequestCode == generatedCode));

            // 5. Tạo entity
            var newRequest = new RepairRequest
            {
                RequestCode = generatedCode,
                IssueDescription = request.IssueDescription.Trim(),
                ProductId = request.ProductId,
                CustomerId = request.CustomerId,
                ReceptionistId = request.ReceptionistId,
                Status = RepairStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.RepairRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            // 6. Load navigation properties
            await _context.Entry(newRequest).Reference(r => r.Product).LoadAsync();
            await _context.Entry(newRequest).Reference(r => r.Customer).LoadAsync();
            if (newRequest.ReceptionistId.HasValue)
                await _context.Entry(newRequest).Reference(r => r.Receptionist).LoadAsync();

            return (true, null, MapToResponseDto(newRequest));
        }

        // ==================== ASSIGN TECHNICIAN ====================
        public async Task<(bool Success, string? ErrorMessage, RepairRequestResponseDto? Data)> AssignTechnicianAsync(
            int id, 
            RepairRequestAssignDto request)
        {
            var repairRequest = await _context.RepairRequests.FindAsync(id);
            if (repairRequest == null)
                return (false, $"Không tìm thấy phiếu sửa chữa có Id = {id}", null);

            var technician = await _context.Users.FindAsync(request.TechnicianId);
            if (technician == null)
                return (false, $"Không tìm thấy người dùng có TechnicianId = {request.TechnicianId}!", null);

            if (!technician.Role.Equals("Technician", StringComparison.OrdinalIgnoreCase))
                return (false, $"Người dùng ID = {request.TechnicianId} không phải là Kỹ thuật viên!", null);

            repairRequest.TechnicianId = request.TechnicianId;

            if (repairRequest.Status == RepairStatusEnum.Pending)
                repairRequest.Status = RepairStatusEnum.InProgress;

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
            RepairRequestUpdateStatusDto request)
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
                TechnicianName = request.Technician?.FullName
            };
        }

        #endregion
    }
}