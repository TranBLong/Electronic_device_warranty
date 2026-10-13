using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;

namespace Warranty.Repositories.Implementations.Complete;

public sealed class RepairRequestAttachmentRepository(AppDbContext dbContext) : IRepairRequestAttachmentRepository
{
    public async Task<IReadOnlyList<RepairRequestAttachment>> GetByRepairRequestIdAsync(
        int repairRequestId,
        CancellationToken cancellationToken = default) =>
        await dbContext.RepairRequestAttachments.AsNoTracking()
            .Include(attachment => attachment.UploadedBy)
            .Where(attachment => attachment.RepairRequestId == repairRequestId)
            .OrderByDescending(attachment => attachment.UploadedAt)
            .ThenByDescending(attachment => attachment.Id)
            .ToListAsync(cancellationToken);

    public Task<RepairRequestAttachment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.RepairRequestAttachments.AsNoTracking()
            .Include(attachment => attachment.UploadedBy)
            .Include(attachment => attachment.RepairRequest)
                .ThenInclude(request => request.WarrantyCard)
            .SingleOrDefaultAsync(attachment => attachment.Id == id, cancellationToken);

    public async Task AddAsync(RepairRequestAttachment attachment, CancellationToken cancellationToken = default) =>
        await dbContext.RepairRequestAttachments.AddAsync(attachment, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
