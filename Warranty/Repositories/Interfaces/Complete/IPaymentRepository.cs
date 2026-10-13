using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Complete;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> GetByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IRepositoryTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default);
}
