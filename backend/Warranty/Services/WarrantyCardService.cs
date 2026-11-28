using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Services;

public sealed class WarrantyCardService(AppDbContext dbContext) : IWarrantyCardService
{
    public async Task<ServiceResult<WarrantyCardResponse>> CreateAsync(
        CreateWarrantyCardRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StartDate == default)
        {
            return ServiceResult<WarrantyCardResponse>.Failure(
                ServiceErrorCode.Invalid, "Start date is required.");
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(
            candidate => candidate.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<WarrantyCardResponse>.Failure(
                ServiceErrorCode.Invalid, "Product was not found.");
        }

        var customer = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == request.CustomerId &&
                         candidate.Role == UserRole.Customer &&
                         candidate.IsActive,
            cancellationToken);
        if (customer is null)
        {
            return ServiceResult<WarrantyCardResponse>.Failure(
                ServiceErrorCode.Invalid, "An active Customer account is required.");
        }

        var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var card = new WarrantyCard
        {
            ProductId = product.Id,
            CustomerId = customer.Id,
            StartDate = startDate,
            EndDate = startDate.AddMonths(product.WarrantyMonths),
            Status = WarrantyStatus.Active,
            Product = product,
            Customer = customer
        };
        dbContext.WarrantyCards.Add(card);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<WarrantyCardResponse>.Success(WarrantyCardMapping.ToResponse(card));
    }

    public async Task<ServiceResult<WarrantyCardResponse>> UpdateAsync(
        int warrantyCardId,
        UpdateWarrantyCardRequest request,
        CancellationToken cancellationToken)
    {
        var card = await dbContext.WarrantyCards
            .Include(candidate => candidate.Product)
            .Include(candidate => candidate.Customer)
            .SingleOrDefaultAsync(candidate => candidate.Id == warrantyCardId, cancellationToken);
        if (card is null)
        {
            return ServiceResult<WarrantyCardResponse>.Failure(
                ServiceErrorCode.NotFound, "Warranty card was not found.");
        }

        if (request.Status is not null)
        {
            if (!Enum.IsDefined(request.Status.Value))
            {
                return ServiceResult<WarrantyCardResponse>.Failure(
                    ServiceErrorCode.Invalid, "Invalid warranty status.");
            }

            card.Status = request.Status.Value;
        }

        if (request.StartDate is not null)
        {
            card.StartDate = DateTime.SpecifyKind(request.StartDate.Value.Date, DateTimeKind.Utc);
            card.EndDate = card.StartDate.AddMonths(card.Product.WarrantyMonths);
        }

        if (request.EndDate is not null)
        {
            var end = DateTime.SpecifyKind(request.EndDate.Value.Date, DateTimeKind.Utc);
            if (end < card.StartDate)
            {
                return ServiceResult<WarrantyCardResponse>.Failure(
                    ServiceErrorCode.Invalid, "End date must be on or after start date.");
            }

            card.EndDate = end;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<WarrantyCardResponse>.Success(WarrantyCardMapping.ToResponse(card));
    }
}

internal static class WarrantyCardMapping
{
    public static WarrantyCardResponse ToResponse(WarrantyCard card) => new(
        card.Id,
        card.ProductId,
        card.Product.Name,
        card.Product.SerialNumber,
        card.CustomerId,
        card.Customer.FullName,
        card.StartDate,
        card.EndDate,
        card.Status == WarrantyStatus.Active && card.EndDate.Date < DateTime.UtcNow.Date
            ? WarrantyStatus.Expired
            : card.Status);
}
