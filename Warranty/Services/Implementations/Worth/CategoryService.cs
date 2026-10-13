using Microsoft.EntityFrameworkCore;
using Warranty.Dtos;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Worth;
using Warranty.Services.Interfaces.Worth;

namespace Warranty.Services.Implementations.Worth;

public sealed class CategoryService(ICategoryRepository categoryRepository) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponseDto>> GetAllAsync(
        string? name,
        CancellationToken cancellationToken = default) =>
        (await categoryRepository.GetAllAsync(name?.Trim(), cancellationToken))
        .Select(ToResponse)
        .ToList();

    public async Task<CategoryResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ToResponse(await GetCategoryAsync(id, cancellationToken));

    public async Task<CategoryResponseDto> CreateAsync(
        CategoryCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var name = ValidateName(request.Name);
        var description = ValidateDescription(request.Description);
        if (await categoryRepository.ExistsByNameAsync(name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("A category with this name already exists.");
        }

        var category = new Category { Name = name, Description = description };
        await categoryRepository.AddAsync(category, cancellationToken);
        try
        {
            await categoryRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await categoryRepository.ExistsByNameAsync(name, cancellationToken: cancellationToken))
            {
                throw new ConflictException("A category with this name already exists.");
            }

            throw;
        }

        return ToResponse(category);
    }

    public async Task<CategoryResponseDto> UpdateAsync(
        int id,
        CategoryUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var category = await GetCategoryAsync(id, cancellationToken, tracking: true);
        var name = ValidateName(request.Name);
        if (await categoryRepository.ExistsByNameAsync(name, id, cancellationToken))
        {
            throw new ConflictException("A category with this name already exists.");
        }

        category.Name = name;
        category.Description = ValidateDescription(request.Description);
        try
        {
            await categoryRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await categoryRepository.ExistsByNameAsync(name, id, cancellationToken))
            {
                throw new ConflictException("A category with this name already exists.");
            }

            throw;
        }

        return ToResponse(category);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await GetCategoryAsync(id, cancellationToken, tracking: true);
        if (await categoryRepository.HasPartsAsync(id, cancellationToken))
        {
            throw new ConflictException("The category cannot be deleted while it has parts.");
        }

        if (await categoryRepository.HasProductsAsync(id, cancellationToken))
        {
            throw new ConflictException("The category cannot be deleted while it is assigned to products.");
        }

        categoryRepository.Remove(category);
        await categoryRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> GetCategoryAsync(
        int id,
        CancellationToken cancellationToken,
        bool tracking = false) =>
        await categoryRepository.GetByIdAsync(id, tracking, cancellationToken)
        ?? throw new NotFoundException("Category was not found.");

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException("Category name is required and must not exceed 100 characters.");
        }

        var value = name.Trim();
        if (value.Length > 100)
        {
            throw new BusinessRuleException("Category name is required and must not exceed 100 characters.");
        }

        return value;
    }

    private static string? ValidateDescription(string? description)
    {
        var value = description?.Trim();
        if (value?.Length > 500)
        {
            throw new BusinessRuleException("Category description must not exceed 500 characters.");
        }

        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static CategoryResponseDto ToResponse(Category category) => new(
        category.Id,
        category.Name,
        category.Description,
        category.IsActive,
        category.CreatedAt);
}
