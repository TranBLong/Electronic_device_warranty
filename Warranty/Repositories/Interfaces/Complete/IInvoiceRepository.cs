using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Repositories.Interfaces.Complete;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByRepairRequestIdAsync(int repairRequestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetAllAsync(InvoiceStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetForCustomerAsync(int customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(InvoiceStatus? status, int? customerId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForRepairRequestAsync(int repairRequestId, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
