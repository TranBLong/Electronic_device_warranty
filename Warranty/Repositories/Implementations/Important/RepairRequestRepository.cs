using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Dtos;
using Warranty.Repositories.Interfaces.Important;

namespace Warranty.Repositories.Implementations.Important;

public sealed class RepairRequestRepository(AppDbContext dbContext) : IRepairRequestRepository
{
    public async Task<IReadOnlyList<RepairRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await WithDetails(dbContext.RepairRequests.AsNoTracking())
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RepairRequest>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default) =>
        await WithDetails(dbContext.RepairRequests.AsNoTracking())
            .Where(request => request.WarrantyCard.CustomerId == customerId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RepairRequest>> GetByTechnicianIdAsync(
        int technicianId,
        CancellationToken cancellationToken = default) =>
        await WithDetails(dbContext.RepairRequests.AsNoTracking())
            .Where(request => request.TechnicianId == technicianId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RepairRequest>> SearchAsync(
        int? customerId, int? assignedTechnicianId, RepairRequestStatus? status, int? filterTechnicianId,
        DateTime? createdFrom, DateTime? createdTo, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySearchFilter(WithDetails(dbContext.RepairRequests.AsNoTracking()), customerId,
            assignedTechnicianId, status, filterTechnicianId, createdFrom, createdTo);
        return await query.OrderByDescending(request => request.CreatedAt).ThenByDescending(request => request.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    }

    public Task<int> CountSearchAsync(
        int? customerId, int? assignedTechnicianId, RepairRequestStatus? status, int? filterTechnicianId,
        DateTime? createdFrom, DateTime? createdTo, CancellationToken cancellationToken = default) =>
        ApplySearchFilter(dbContext.RepairRequests.AsNoTracking(), customerId, assignedTechnicianId,
            status, filterTechnicianId, createdFrom, createdTo).CountAsync(cancellationToken);

    public Task<RepairRequest?> GetByIdAsync(
        int id,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.RepairRequests.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return WithDetails(query).SingleOrDefaultAsync(request => request.Id == id, cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        dbContext.RepairRequests.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountByStatusAsync(
        RepairRequestStatus status,
        CancellationToken cancellationToken = default) =>
        dbContext.RepairRequests.AsNoTracking().CountAsync(request => request.Status == status, cancellationToken);

    public async Task AddAsync(RepairRequest repairRequest, CancellationToken cancellationToken = default) =>
        await dbContext.RepairRequests.AddAsync(repairRequest, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task SaveStatusChangeAsync(
        RepairRequest repairRequest,
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TechnicianPerformanceItem>> GetTechnicianPerformanceAsync(
        CancellationToken cancellationToken = default)
    {
        var durations = await dbContext.RepairRequests.AsNoTracking()
            .Where(request => request.TechnicianId != null && request.Status == RepairRequestStatus.Completed)
            .Select(request => new
            {
                TechnicianId = request.TechnicianId!.Value,
                StartedAt = request.StatusHistory
                    .Where(history => history.NewStatus == RepairRequestStatus.InProgress ||
                                      history.NewStatus == RepairRequestStatus.Received)
                    .OrderBy(history => history.ChangedAt)
                    .Select(history => (DateTime?)history.ChangedAt).FirstOrDefault(),
                CompletedAt = request.StatusHistory
                    .Where(history => history.NewStatus == RepairRequestStatus.Completed)
                    .OrderByDescending(history => history.ChangedAt)
                    .Select(history => (DateTime?)history.ChangedAt).FirstOrDefault()
            }).ToListAsync(cancellationToken);

        var averages = durations
            .Where(row => row.StartedAt.HasValue && row.CompletedAt.HasValue && row.CompletedAt >= row.StartedAt)
            .GroupBy(row => row.TechnicianId)
            .ToDictionary(group => group.Key,
                group => group.Average(row => (row.CompletedAt!.Value - row.StartedAt!.Value).TotalHours));

        var technicians = await dbContext.Users.AsNoTracking()
            .Where(user => user.Role == UserRole.Technician)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                AssignedCount = dbContext.RepairRequests.Count(request => request.TechnicianId == user.Id),
                CompletedCount = dbContext.RepairRequests.Count(request =>
                    request.TechnicianId == user.Id && request.Status == RepairRequestStatus.Completed)
            })
            .OrderBy(user => user.FullName).ToListAsync(cancellationToken);

        return technicians.Select(user => new TechnicianPerformanceItem(
            user.Id, user.FullName, user.AssignedCount, user.CompletedCount,
            averages.TryGetValue(user.Id, out var hours) ? hours : null)).ToList();
    }

    private static IQueryable<RepairRequest> WithDetails(IQueryable<RepairRequest> query) =>
        query.Include(request => request.WarrantyCard)
            .ThenInclude(card => card.Customer)
            .Include(request => request.Technician)
            .Include(request => request.StatusHistory.OrderBy(history => history.ChangedAt));

    private static IQueryable<RepairRequest> ApplySearchFilter(
        IQueryable<RepairRequest> query, int? customerId, int? assignedTechnicianId, RepairRequestStatus? status,
        int? filterTechnicianId, DateTime? createdFrom, DateTime? createdTo)
    {
        if (customerId.HasValue) query = query.Where(request => request.WarrantyCard.CustomerId == customerId.Value);
        if (assignedTechnicianId.HasValue) query = query.Where(request => request.TechnicianId == assignedTechnicianId.Value);
        if (status.HasValue) query = query.Where(request => request.Status == status.Value);
        if (filterTechnicianId.HasValue) query = query.Where(request => request.TechnicianId == filterTechnicianId.Value);
        if (createdFrom.HasValue) query = query.Where(request => request.CreatedAt >= createdFrom.Value);
        if (createdTo.HasValue) query = query.Where(request => request.CreatedAt <= createdTo.Value);
        return query;
    }
}
