using Microsoft.EntityFrameworkCore;
using Warranty.Dtos;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Worth;
using Warranty.Services.Interfaces.Worth;

namespace Warranty.Services.Implementations.Worth;

public sealed class PartService(IPartRepository partRepository, ICategoryRepository categoryRepository) : IPartService
{
    public async Task<PartListResponseDto> GetAllAsync(
        int? categoryId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        var (items, totalCount) = await partRepository.GetAllAsync(
            categoryId, search?.Trim(), page, pageSize, cancellationToken);
        return new PartListResponseDto(items.Select(ToResponse).ToList(), page, pageSize, totalCount);
    }

    public async Task<PartResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ToResponse(await GetPartAsync(id, cancellationToken));

    public async Task<PartResponseDto> CreateAsync(
        PartCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var values = Validate(request.CategoryId, request.Name, request.PartNumber, request.Description,
            request.UnitPrice, request.StockQuantity);
        var category = await GetCategoryAsync(request.CategoryId, cancellationToken);
        if (await partRepository.ExistsByPartNumberAsync(values.PartNumber, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A part with this part number already exists.");
        }

        var part = new Part
        {
            CategoryId = category.Id,
            Name = values.Name,
            PartNumber = values.PartNumber,
            Description = values.Description,
            UnitPrice = values.UnitPrice,
            StockQuantity = values.StockQuantity
        };
        await partRepository.AddAsync(part, cancellationToken);
        try
        {
            await partRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await partRepository.ExistsByPartNumberAsync(
                values.PartNumber, cancellationToken: cancellationToken))
            {
                throw new ConflictException("A part with this part number already exists.");
            }

            throw;
        }

        return ToResponse(await GetPartAsync(part.Id, cancellationToken));
    }

    public async Task<PartResponseDto> UpdateAsync(
        int id,
        PartUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var part = await GetPartAsync(id, cancellationToken, tracking: true);
        var values = Validate(request.CategoryId, request.Name, request.PartNumber, request.Description,
            request.UnitPrice, request.StockQuantity);
        var category = await GetCategoryAsync(request.CategoryId, cancellationToken);
        if (await partRepository.ExistsByPartNumberAsync(values.PartNumber, id, cancellationToken))
        {
            throw new ConflictException("A part with this part number already exists.");
        }

        part.CategoryId = category.Id;
        part.Name = values.Name;
        part.PartNumber = values.PartNumber;
        part.Description = values.Description;
        part.UnitPrice = values.UnitPrice;
        part.StockQuantity = values.StockQuantity;
        if (request.IsActive.HasValue)
        {
            part.IsActive = request.IsActive.Value;
        }

        try
        {
            await partRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await partRepository.ExistsByPartNumberAsync(values.PartNumber, id, cancellationToken))
            {
                throw new ConflictException("A part with this part number already exists.");
            }

            throw;
        }

        return ToResponse(await GetPartAsync(id, cancellationToken));
    }

    public async Task<PartResponseDto> UpdateStockAsync(
        int id,
        PartStockUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.StockQuantity < 0)
        {
            throw new BusinessRuleException("Stock quantity cannot be negative.");
        }

        var part = await GetPartAsync(id, cancellationToken, tracking: true);
        part.StockQuantity = request.StockQuantity;
        await partRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(part);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var part = await GetPartAsync(id, cancellationToken, tracking: true);
        if (await partRepository.HasRepairRequestPartsAsync(id, cancellationToken))
        {
            throw new ConflictException("The part cannot be deleted because it has been used in a repair request.");
        }

        partRepository.Remove(part);
        await partRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Part> GetPartAsync(
        int id,
        CancellationToken cancellationToken,
        bool tracking = false) =>
        await partRepository.GetByIdAsync(id, tracking, cancellationToken)
        ?? throw new NotFoundException("Part was not found.");

    private async Task<Category> GetCategoryAsync(int id, CancellationToken cancellationToken) =>
        await categoryRepository.GetByIdAsync(id, cancellationToken: cancellationToken)
        ?? throw new NotFoundException("Category was not found.");

    private static (string Name, string PartNumber, string? Description, decimal UnitPrice, int StockQuantity)
        Validate(
            int categoryId,
            string name,
            string partNumber,
            string? description,
            decimal unitPrice,
            int stockQuantity)
    {
        if (categoryId <= 0)
        {
            throw new BusinessRuleException("A valid category is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException("Part name is required and must not exceed 200 characters.");
        }

        var cleanName = name.Trim();
        if (cleanName.Length > 200)
        {
            throw new BusinessRuleException("Part name is required and must not exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(partNumber))
        {
            throw new BusinessRuleException("Part number is required and must not exceed 50 characters.");
        }

        var cleanPartNumber = partNumber.Trim();
        if (cleanPartNumber.Length > 50)
        {
            throw new BusinessRuleException("Part number is required and must not exceed 50 characters.");
        }

        var cleanDescription = description?.Trim();
        if (cleanDescription?.Length > 1000)
        {
            throw new BusinessRuleException("Part description must not exceed 1000 characters.");
        }

        if (unitPrice < 0)
        {
            throw new BusinessRuleException("Unit price cannot be negative.");
        }

        if (stockQuantity < 0)
        {
            throw new BusinessRuleException("Stock quantity cannot be negative.");
        }

        return (cleanName, cleanPartNumber, string.IsNullOrEmpty(cleanDescription) ? null : cleanDescription,
            unitPrice, stockQuantity);
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1
            || pageSize is < 1 or > 100
            || (long)(page - 1) * pageSize > int.MaxValue)
        {
            throw new BusinessRuleException(
                "Page must be at least 1 and page size must be between 1 and 100; the offset must be valid.");
        }
    }

    private static PartResponseDto ToResponse(Part part) => new(
        part.Id,
        part.CategoryId,
        part.Category.Name,
        part.Name,
        part.PartNumber,
        part.Description,
        part.UnitPrice,
        part.StockQuantity,
        part.IsActive,
        part.CreatedAt);
}
