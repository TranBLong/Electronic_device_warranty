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
    public async Task<PagedResult<WarrantyCardResponse>> GetAllAsync(
        int userId,
        UserRole role,
        WarrantyStatus? status,
        string? keyword,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        int? customerId = role switch
        {
            UserRole.Customer => userId,
            UserRole.Admin or UserRole.Receptionist => null,
            _ => throw new ForbiddenException("You are not allowed to view warranty cards.")
        };
        var cards = await warrantyCardRepository.SearchAsync(customerId, status, keyword, page, pageSize, cancellationToken);
        var total = await warrantyCardRepository.CountSearchAsync(customerId, status, keyword, cancellationToken);
        return new PagedResult<WarrantyCardResponse>(cards.Select(ToResponse).ToList(), page, pageSize, total,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new BusinessRuleException("Page must be positive and pageSize must be between 1 and 100.");
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

    public async Task<WarrantyCardResponse> VoidAsync(
        int id,
        VoidWarrantyCardRequest? request,
        int actorId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role is not (UserRole.Receptionist or UserRole.Admin))
            throw new ForbiddenException("Only Receptionists and Admins can void warranty cards.");

        var card = await warrantyCardRepository.GetByIdAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Warranty card was not found.");
        if (card.Status == WarrantyStatus.Voided)
            throw new ConflictException("The warranty card has already been voided.");
        if (card.Status != WarrantyStatus.Active)
            throw new BusinessRuleException("Only Active warranty cards can be voided.");
        if (await warrantyCardRepository.HasBlockingRepairRequestsAsync(id, cancellationToken))
            throw new BusinessRuleException("Cannot void a warranty card that still has repair requests in Received, InProgress or Completed status.");

        var reason = request?.Reason?.Trim();
        if (reason?.Length > 500)
            throw new BusinessRuleException("Void reason must not exceed 500 characters.");

        card.Status = WarrantyStatus.Voided;
        card.VoidedAt = DateTime.UtcNow;
        card.VoidReason = string.IsNullOrEmpty(reason) ? null : reason;
        var notification = new Notification
        {
            UserId = card.CustomerId,
            Type = NotificationType.General,
            Title = $"Warranty card #{card.Id} has been voided",
            Message = string.IsNullOrEmpty(reason)
                ? $"Your warranty card #{card.Id} has been voided."
                : $"Your warranty card #{card.Id} has been voided. Reason: {reason}"
        };

        await warrantyCardRepository.VoidAsync(card, notification, cancellationToken);
        return ToResponse(card);
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
            : card.Status,
        card.VoidedAt,
        card.VoidReason);
}
