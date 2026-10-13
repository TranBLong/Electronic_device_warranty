using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;

namespace Warranty.Repositories.Implementations.Complete;

public sealed class ServiceCenterRepository(AppDbContext dbContext) : IServiceCenterRepository
{
    public async Task<IReadOnlyList<ServiceCenter>> GetAllAsync(
        string? name,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ServiceCenters.AsNoTracking().Include(center => center.Manager).AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(center => center.Name.Contains(name));
        }

        if (isActive.HasValue)
        {
            query = query.Where(center => center.IsActive == isActive.Value);
        }

        return await query.OrderBy(center => center.Name).ToListAsync(cancellationToken);
    }

    public Task<ServiceCenter?> GetByIdAsync(
        int id,
        bool tracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ServiceCenters.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.Include(center => center.Manager).SingleOrDefaultAsync(center => center.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        int? exceptId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.ServiceCenters.AsNoTracking()
            .AnyAsync(center => center.Name == name && (!exceptId.HasValue || center.Id != exceptId), cancellationToken);

    public Task<bool> HasRepairRequestsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.RepairRequests.AsNoTracking().AnyAsync(request => request.ServiceCenterId == id, cancellationToken);

    public Task<bool> HasStaffAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().AnyAsync(user => user.ServiceCenterId == id, cancellationToken);

    public async Task AddAsync(ServiceCenter center, CancellationToken cancellationToken = default) =>
        await dbContext.ServiceCenters.AddAsync(center, cancellationToken);

    public void Remove(ServiceCenter center) => dbContext.ServiceCenters.Remove(center);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
