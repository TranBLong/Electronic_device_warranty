using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products.AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => ToResponse(product))
            .ToListAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var serialNumber = NormalizeSerialNumber(request.SerialNumber);
        if (await dbContext.Products.AnyAsync(product => product.SerialNumber == serialNumber, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            SerialNumber = serialNumber,
            WarrantyMonths = request.WarrantyMonths
        };
        dbContext.Products.Add(product);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });
        }

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(
        int id,
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var serialNumber = NormalizeSerialNumber(request.SerialNumber);
        if (await dbContext.Products.AnyAsync(
                candidate => candidate.Id != id && candidate.SerialNumber == serialNumber,
                cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });
        }

        product.Name = request.Name.Trim();
        product.Brand = request.Brand.Trim();
        product.Model = request.Model.Trim();
        product.SerialNumber = serialNumber;
        product.WarrantyMonths = request.WarrantyMonths;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });
        }

        return Ok(ToResponse(product));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        if (await dbContext.WarrantyCards.AnyAsync(card => card.ProductId == id, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Products referenced by warranty cards cannot be deleted." });
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string NormalizeSerialNumber(string serialNumber) => serialNumber.Trim().ToUpperInvariant();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.Name,
        product.Brand,
        product.Model,
        product.SerialNumber,
        product.WarrantyMonths,
        product.CreatedAt);
}
