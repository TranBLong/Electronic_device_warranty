using Warranty.Dtos;

namespace Warranty.Services;

public interface IWarrantyCardService
{
    Task<ServiceResult<WarrantyCardResponse>> CreateAsync(
        CreateWarrantyCardRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<WarrantyCardResponse>> UpdateAsync(
        int warrantyCardId,
        UpdateWarrantyCardRequest request,
        CancellationToken cancellationToken);
}
