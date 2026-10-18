using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Warranty.Data;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;

namespace Warranty.Repositories.Implementations.Complete;

public sealed class PaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> GetByInvoiceIdAsync(
        int invoiceId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Payments.AsNoTracking()
            .Include(payment => payment.ReceivedBy)
            .Where(payment => payment.InvoiceId == invoiceId)
            .OrderBy(payment => payment.PaidAt)
            .ThenBy(payment => payment.Id)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetTotalByInvoiceIdAsync(
        int invoiceId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Payments.AsNoTracking()
            .Where(payment => payment.InvoiceId == invoiceId)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken)
            ?? 0m;

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
        await dbContext.Payments.AddAsync(payment, cancellationToken);

    public async Task AddNotificationAsync(
        Notification notification,
        CancellationToken cancellationToken = default) =>
        await dbContext.Notifications.AddAsync(notification, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IRepositoryTransaction> BeginSerializableTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        return new RepositoryTransaction(transaction);
    }

    private sealed class RepositoryTransaction(IDbContextTransaction transaction) : IRepositoryTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default) =>
            transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
