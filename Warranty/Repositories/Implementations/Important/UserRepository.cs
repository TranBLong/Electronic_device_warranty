using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Important;

namespace Warranty.Repositories.Implementations.Important;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Users.AsNoTracking().OrderBy(user => user.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> GetActiveByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default) =>
        await dbContext.Users.AsNoTracking()
            .Where(user => user.Role == role && user.IsActive)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);

    public Task<bool> EmailExistsAsync(
        string email,
        int? exceptUserId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().AnyAsync(
            user => user.Email == email && (!exceptUserId.HasValue || user.Id != exceptUserId.Value),
            cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await dbContext.Users.AddAsync(user, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
