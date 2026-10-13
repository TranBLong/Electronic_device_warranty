using Microsoft.AspNetCore.Http;
using Warranty.Dtos;
using Warranty.Enums;

namespace Warranty.Services.Interfaces.Complete;

public interface IRepairRequestAttachmentService
{
    Task<RepairRequestAttachmentResponseDto> UploadAsync(int repairRequestId, IFormFile file, int userId, UserRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepairRequestAttachmentResponseDto>> GetByRepairRequestIdAsync(int repairRequestId, int userId, UserRole role, CancellationToken cancellationToken = default);
    Task<AttachmentDownloadDto> GetDownloadAsync(int id, int userId, UserRole role, CancellationToken cancellationToken = default);
}
