using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
// QueryParameters in Dtos
using Warranty.Models;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        CancellationToken cancellationToken)
    {
        var q = dbContext.Products.AsNoTracking().Include(product => product.Category).AsQueryable();
        if (query.NormalizedKeyword is { } term)
        {
            q = q.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Brand.ToLower().Contains(term) ||
                p.Model.ToLower().Contains(term) ||
                p.SerialNumber.ToLower().Contains(term));
        }
        var sortKey = (query.SortBy ?? "name").ToLowerInvariant();
        var desc = query.IsDescending;
        q = sortKey switch
        {
            "brand" => desc ? q.OrderByDescending(p => p.Brand) : q.OrderBy(p => p.Brand),
            "model" => desc ? q.OrderByDescending(p => p.Model) : q.OrderBy(p => p.Model),
            "serial" or "serialnumber" => desc ? q.OrderByDescending(p => p.SerialNumber) : q.OrderBy(p => p.SerialNumber),
            "warranty" or "warrantymonths" => desc ? q.OrderByDescending(p => p.WarrantyMonths) : q.OrderBy(p => p.WarrantyMonths),
            "created" or "createdat" => desc ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
            _ => desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name)
        };
        var total = await q.CountAsync(cancellationToken);
        var list = await q.Skip(query.Skip).Take(query.Take).ToListAsync(cancellationToken);
        return Ok(PagedResult<ProductResponse>.Create(list.Select(ToResponse).ToList(), query.PageNumber, query.Take, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking()
            .Include(candidate => candidate.Category)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    [Authorize(Policy = Policies.CanManageCatalog)]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        var serialNumber = NormalizeSerialNumber(request.SerialNumber);
        if (await dbContext.Products.AnyAsync(product => product.SerialNumber == serialNumber, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });
        }

        if (request.CategoryId is int catId)
        {
            var catExists = await dbContext.Categories.AnyAsync(c => c.Id == catId && c.IsActive, cancellationToken);
            if (!catExists)
                return BadRequest(new ProblemDetails { Title = "Category was not found or inactive." });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            SerialNumber = serialNumber,
            WarrantyMonths = request.WarrantyMonths,
            CategoryId = request.CategoryId
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

    [Authorize(Policy = Policies.CanManageCatalog)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(int id, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (product is null) return NotFound();

        var serialNumber = NormalizeSerialNumber(request.SerialNumber);
        if (await dbContext.Products.AnyAsync(
                candidate => candidate.Id != id && candidate.SerialNumber == serialNumber, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });
        }

        if (request.CategoryId is int catIdUpd)
        {
            var catExists = await dbContext.Categories.AnyAsync(c => c.Id == catIdUpd && c.IsActive, cancellationToken);
            if (!catExists)
                return BadRequest(new ProblemDetails { Title = "Category was not found or inactive." });
        }

        product.Name = request.Name.Trim();
        product.Brand = request.Brand.Trim();
        product.Model = request.Model.Trim();
        product.SerialNumber = serialNumber;
        product.WarrantyMonths = request.WarrantyMonths;
        product.CategoryId = request.CategoryId;
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

    [Authorize(Policy = Policies.CanManageCatalog)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (product is null) return NotFound();

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
        product.CategoryId,
        product.Category?.Name,
        product.CreatedAt);
}