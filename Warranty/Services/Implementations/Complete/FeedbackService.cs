using Microsoft.EntityFrameworkCore;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Services.Implementations.Complete;

public sealed class FeedbackService(
    IFeedbackRepository feedbackRepository,
    IRepairRequestRepository repairRequestRepository) : IFeedbackService
{
    public async Task<FeedbackResponseDto> CreateAsync(
        int repairRequestId,
        FeedbackCreateDto request,
        int customerId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (role != UserRole.Customer)
        {
            throw new ForbiddenException("Only Customers can submit feedback.");
        }

        if (request.Rating is < 1 or > 5)
        {
            throw new BusinessRuleException("Rating must be between 1 and 5.");
        }

        var repairRequest = await repairRequestRepository.GetByIdAsync(repairRequestId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException("Repair request was not found.");
        if (repairRequest.WarrantyCard.CustomerId != customerId)
        {
            throw new ForbiddenException("You can submit feedback only for your own repair requests.");
        }

        if (repairRequest.Status != RepairRequestStatus.Returned)
        {
            throw new BusinessRuleException("Feedback can be submitted only after the repair request is Returned.");
        }

        if (await feedbackRepository.GetByRepairRequestIdAsync(repairRequestId, cancellationToken) is not null)
        {
            throw new ConflictException("Feedback has already been submitted for this repair request.");
        }

        var comment = request.Comment?.Trim();
        if (comment?.Length > 1000)
        {
            throw new BusinessRuleException("Feedback comment must not exceed 1000 characters.");
        }

        var feedback = new Feedback
        {
            RepairRequestId = repairRequestId,
            CustomerId = customerId,
            Rating = request.Rating,
            Comment = string.IsNullOrEmpty(comment) ? null : comment,
            CreatedAt = DateTime.UtcNow
        };
        await feedbackRepository.AddAsync(feedback, cancellationToken);
        try
        {
            await feedbackRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await feedbackRepository.GetByRepairRequestIdAsync(repairRequestId, cancellationToken) is not null)
            {
                throw new ConflictException("Feedback has already been submitted for this repair request.");
            }

            throw;
        }

        return new FeedbackResponseDto(
            feedback.Id, feedback.RepairRequestId, feedback.CustomerId,
            repairRequest.WarrantyCard.Customer.FullName, feedback.Rating, feedback.Comment, feedback.CreatedAt);
    }

    public async Task<FeedbackResponseDto> GetByRepairRequestIdAsync(
        int repairRequestId,
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var feedback = await feedbackRepository.GetByRepairRequestIdAsync(repairRequestId, cancellationToken)
            ?? throw new NotFoundException("Feedback was not found.");
        if (role is UserRole.Admin or UserRole.Manager)
        {
            return ToResponse(feedback);
        }

        if (role == UserRole.Customer && feedback.CustomerId == userId)
        {
            return ToResponse(feedback);
        }

        throw new ForbiddenException("You are not allowed to view this feedback.");
    }

    public async Task<PagedResponseDto<FeedbackResponseDto>> GetAllAsync(
        int? rating,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
        {
            throw new BusinessRuleException("Page must be positive and pageSize must be between 1 and 100.");
        }

        if (rating is < 1 or > 5)
        {
            throw new BusinessRuleException("Rating filter must be between 1 and 5.");
        }

        var feedback = await feedbackRepository.GetAllAsync(rating, page, pageSize, cancellationToken);
        var count = await feedbackRepository.CountAsync(rating, cancellationToken);
        return new PagedResponseDto<FeedbackResponseDto>(feedback.Select(ToResponse).ToList(), page, pageSize, count);
    }

    private static FeedbackResponseDto ToResponse(Feedback feedback) => new(
        feedback.Id,
        feedback.RepairRequestId,
        feedback.CustomerId,
        feedback.Customer.FullName,
        feedback.Rating,
        feedback.Comment,
        feedback.CreatedAt);
}
