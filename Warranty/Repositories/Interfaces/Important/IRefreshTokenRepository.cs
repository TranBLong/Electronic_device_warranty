using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Important;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task MarkActiveAsRevokedAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> RunInSerializableTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);
}
