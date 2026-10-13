using Microsoft.EntityFrameworkCore;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Complete;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Services.Interfaces.Complete;

namespace Warranty.Services.Implementations.Complete;

public sealed class ServiceCenterService(
    IServiceCenterRepository serviceCenterRepository,
    IUserRepository userRepository) : IServiceCenterService
{
    public async Task<IReadOnlyList<ServiceCenterResponseDto>> GetAllAsync(
        string? name,
        bool? isActive,
        CancellationToken cancellationToken = default) =>
        (await serviceCenterRepository.GetAllAsync(name?.Trim(), isActive, cancellationToken))
        .Select(ToResponse)
        .ToList();

    public async Task<ServiceCenterResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ToResponse(await GetCenterAsync(id, cancellationToken));

    public async Task<ServiceCenterResponseDto> CreateAsync(
        ServiceCenterCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var name = ValidateRequired(request.Name, 200, "Service center name");
        var address = ValidateRequired(request.Address, 500, "Service center address");
        var phone = ValidateOptional(request.Phone, 30, "Phone");
        await ValidateManagerAsync(request.ManagerId, cancellationToken);
        if (await serviceCenterRepository.ExistsByNameAsync(name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A service center with this name already exists.");
        }

        var center = new ServiceCenter
        {
            Name = name,
            Address = address,
            Phone = phone,
            ManagerId = request.ManagerId
        };
        await serviceCenterRepository.AddAsync(center, cancellationToken);
        try
        {
            await serviceCenterRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await serviceCenterRepository.ExistsByNameAsync(name, cancellationToken: cancellationToken))
            {
                throw new ConflictException("A service center with this name already exists.");
            }

            throw;
        }

        return ToResponse(await GetCenterAsync(center.Id, cancellationToken));
    }

    public async Task<ServiceCenterResponseDto> UpdateAsync(
        int id,
        ServiceCenterUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var center = await GetCenterAsync(id, cancellationToken, tracking: true);
        var name = ValidateRequired(request.Name, 200, "Service center name");
        if (await serviceCenterRepository.ExistsByNameAsync(name, id, cancellationToken))
        {
            throw new ConflictException("A service center with this name already exists.");
        }

        await ValidateManagerAsync(request.ManagerId, cancellationToken);
        center.Name = name;
        center.Address = ValidateRequired(request.Address, 500, "Service center address");
        center.Phone = ValidateOptional(request.Phone, 30, "Phone");
        center.ManagerId = request.ManagerId;
        await serviceCenterRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(await GetCenterAsync(id, cancellationToken));
    }

    public async Task<ServiceCenterResponseDto> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var center = await GetCenterAsync(id, cancellationToken, tracking: true);
        center.IsActive = isActive;
        await serviceCenterRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(await GetCenterAsync(id, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var center = await GetCenterAsync(id, cancellationToken, tracking: true);
        if (await serviceCenterRepository.HasRepairRequestsAsync(id, cancellationToken))
        {
            throw new ConflictException("A service center used by repair requests cannot be deleted.");
        }

        if (await serviceCenterRepository.HasStaffAsync(id, cancellationToken))
        {
            throw new ConflictException("A service center assigned to staff cannot be deleted.");
        }

        serviceCenterRepository.Remove(center);
        await serviceCenterRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateManagerAsync(int managerId, CancellationToken cancellationToken)
    {
        var manager = await userRepository.GetByIdAsync(managerId, cancellationToken: cancellationToken);
        if (manager is null || manager.Role != UserRole.Manager)
        {
            throw new BusinessRuleException("ManagerId must reference a user with the Manager role.");
        }
    }

    private async Task<ServiceCenter> GetCenterAsync(
        int id,
        CancellationToken cancellationToken,
        bool tracking = false) =>
        await serviceCenterRepository.GetByIdAsync(id, tracking, cancellationToken)
        ?? throw new NotFoundException("Service center was not found.");

    private static string ValidateRequired(string? value, int maxLength, string label)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > maxLength)
        {
            throw new BusinessRuleException($"{label} is required and must not exceed {maxLength} characters.");
        }

        return trimmed;
    }

    private static string? ValidateOptional(string? value, int maxLength, string label)
    {
        var trimmed = value?.Trim();
        if (trimmed?.Length > maxLength)
        {
            throw new BusinessRuleException($"{label} must not exceed {maxLength} characters.");
        }

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static ServiceCenterResponseDto ToResponse(ServiceCenter center) => new(
        center.Id, center.Name, center.Address, center.Phone, center.ManagerId, center.Manager.FullName,
        center.IsActive, center.CreatedAt);
}
