using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Services.Implementations.Important;

public sealed class WarrantyCardService(
    IWarrantyCardRepository warrantyCardRepository,
    IProductRepository productRepository,
    IUserRepository userRepository) : IWarrantyCardService
{
    public async Task<IReadOnlyList<WarrantyCardResponse>> GetAllAsync(
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var cards = role switch
        {
            UserRole.Customer => await warrantyCardRepository.GetByCustomerIdAsync(userId, cancellationToken),
            UserRole.Admin or UserRole.Receptionist => await warrantyCardRepository.GetAllAsync(cancellationToken),
            _ => throw new ForbiddenException("You are not allowed to view warranty cards.")
        };
        return cards.Select(ToResponse).ToList();
    }

    public async Task<WarrantyCardResponse> GetByIdAsync(
        int id,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var card = await warrantyCardRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Warranty card was not found.");
        EnsureCanView(card, userId, role);
        return ToResponse(card);
    }

    public async Task<WarrantyCardResponse> CreateAsync(
        CreateWarrantyCardRequest request,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role != UserRole.Receptionist)
        {
            throw new ForbiddenException("Only Receptionists can create warranty cards.");
        }

        if (request.StartDate == default)
        {
            throw new BusinessRuleException("Start date is required.");
        }

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken: cancellationToken)
            ?? throw new BusinessRuleException("Product was not found.");
        var customer = await userRepository.GetByIdAsync(request.CustomerId, cancellationToken: cancellationToken);
        if (customer is null || customer.Role != UserRole.Customer || !customer.IsActive)
        {
            throw new BusinessRuleException("An active Customer account is required.");
        }

        var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var card = new WarrantyCard
        {
            ProductId = product.Id,
            CustomerId = customer.Id,
            StartDate = startDate,
            EndDate = startDate.AddMonths(product.WarrantyMonths),
            Status = WarrantyStatus.Active
        };
        await warrantyCardRepository.AddAsync(card, cancellationToken);
        await warrantyCardRepository.SaveChangesAsync(cancellationToken);

        // The create operation intentionally only sets foreign-key values, so the
        // navigation properties are not populated on the newly-added entity.
        // Reload the saved card with its details before mapping the response.
        var savedCard = await warrantyCardRepository.GetByIdAsync(card.Id, cancellationToken)
            ?? throw new NotFoundException("The created warranty card could not be loaded.");
        return ToResponse(savedCard);
    }

    private static void EnsureCanView(WarrantyCard card, int userId, UserRole role)
    {
        if (role == UserRole.Customer && card.CustomerId == userId)
        {
            return;
        }

        if (role is UserRole.Admin or UserRole.Receptionist)
        {
            return;
        }

        if (role == UserRole.Customer)
        {
            throw new ForbiddenException("You are not allowed to view this warranty card.");
        }

        throw new ForbiddenException("You are not allowed to view warranty cards.");
    }

    private static WarrantyCardResponse ToResponse(WarrantyCard card) => new(
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
