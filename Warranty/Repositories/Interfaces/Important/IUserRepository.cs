using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Important;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetActiveByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, int? exceptUserId = null, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
