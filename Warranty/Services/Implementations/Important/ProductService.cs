using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warranty.Dtos;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Interfaces.Important;
using Warranty.Services.Interfaces.Important;

namespace Warranty.Services.Implementations.Important;

public sealed class ProductService(IProductRepository productRepository) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await productRepository.GetAllAsync(cancellationToken)).Select(ToResponse).ToList();

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ToResponse(await GetProductAsync(id, cancellationToken));

    public async Task<ProductResponse> CreateAsync(
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var serialNumber = NormalizeSerialNumber(request.SerialNumber);
        if (await productRepository.SerialNumberExistsAsync(serialNumber, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Serial number is already registered.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            SerialNumber = serialNumber,
            WarrantyMonths = request.WarrantyMonths
        };
        await productRepository.AddAsync(product, cancellationToken);
        await SaveProductAsync(cancellationToken);
        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(
        int id,
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(id, cancellationToken, tracking: true);
        var serialNumber = NormalizeSerialNumber(request.SerialNumber);
        if (await productRepository.SerialNumberExistsAsync(serialNumber, id, cancellationToken))
        {
            throw new ConflictException("Serial number is already registered.");
        }

        product.Name = request.Name.Trim();
        product.Brand = request.Brand.Trim();
        product.Model = request.Model.Trim();
        product.SerialNumber = serialNumber;
        product.WarrantyMonths = request.WarrantyMonths;
        await SaveProductAsync(cancellationToken);
        return ToResponse(product);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await GetProductAsync(id, cancellationToken, tracking: true);
        if (await productRepository.HasWarrantyCardsAsync(id, cancellationToken))
        {
            throw new ConflictException("Products referenced by warranty cards cannot be deleted.");
        }

        productRepository.Delete(product);
        await productRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Product> GetProductAsync(
        int id,
        CancellationToken cancellationToken,
        bool tracking = false) =>
        await productRepository.GetByIdAsync(id, tracking, cancellationToken)
        ?? throw new NotFoundException("Product was not found.");

    private async Task SaveProductAsync(CancellationToken cancellationToken)
    {
        try
        {
            await productRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ConflictException("Serial number is already registered.");
        }
    }

    private static string NormalizeSerialNumber(string serialNumber) => serialNumber.Trim().ToUpperInvariant();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static ProductResponse ToResponse(Product product) => new(
        product.Id, product.Name, product.Brand, product.Model, product.SerialNumber, product.WarrantyMonths, product.CreatedAt);
}
