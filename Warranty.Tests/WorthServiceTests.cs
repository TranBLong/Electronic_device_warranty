using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Implementations.Complete;
using Warranty.Repositories.Implementations.Important;
using Warranty.Repositories.Implementations.Worth;
using Warranty.Services.Implementations.Worth;

namespace Warranty.Tests;

public sealed class WorthServiceTests
{
    [Fact]
    public async Task AddPart_SavesUsageAndDecrementsStock()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateRepairRequestPartService();

        var response = await service.AddAsync(
            database.RepairRequest.Id,
            new RepairRequestPartCreateDto(database.Part.Id, 2),
            database.Technician.Id,
            UserRole.Technician);

        Assert.Equal("Replacement Part", response.PartName);
        Assert.Equal("PART-001", response.PartNumber);
        Assert.Equal(2, response.Quantity);
        Assert.Equal(12.50m, response.UnitPrice);
        Assert.Equal(25m, response.TotalPrice);
        Assert.Equal(3, await database.Context.Parts.AsNoTracking()
            .Where(part => part.Id == database.Part.Id)
            .Select(part => part.StockQuantity)
            .SingleAsync());
        Assert.Single(await database.Context.RepairRequestParts.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddPart_RejectsNonPositiveQuantity(int quantity)
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateRepairRequestPartService().AddAsync(
            database.RepairRequest.Id,
            new RepairRequestPartCreateDto(database.Part.Id, quantity),
            database.Technician.Id,
            UserRole.Technician));
        Assert.Equal(5, await database.GetStockAsync());
    }

    [Fact]
    public async Task AddPart_RejectsQuantityAboveAvailableStock()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateRepairRequestPartService().AddAsync(
            database.RepairRequest.Id,
            new RepairRequestPartCreateDto(database.Part.Id, 6),
            database.Technician.Id,
            UserRole.Technician));
        Assert.Empty(await database.Context.RepairRequestParts.ToListAsync());
        Assert.Equal(5, await database.GetStockAsync());
    }

    [Fact]
    public async Task AddPart_RejectsRequestOutsideInProgress()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.RepairRequest.Status = RepairRequestStatus.Received;
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateRepairRequestPartService().AddAsync(
            database.RepairRequest.Id,
            new RepairRequestPartCreateDto(database.Part.Id, 1),
            database.Technician.Id,
            UserRole.Technician));
    }

    [Fact]
    public async Task AddPart_RejectsTechnicianNotAssignedToRequest()
    {
        await using var database = await TestDatabase.CreateAsync();
        var otherTechnician = new User
        {
            FullName = "Other Technician",
            Email = "other-technician@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.Technician
        };
        database.Context.Users.Add(otherTechnician);
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => database.CreateRepairRequestPartService().AddAsync(
            database.RepairRequest.Id,
            new RepairRequestPartCreateDto(database.Part.Id, 1),
            otherTechnician.Id,
            UserRole.Technician));
    }

    [Fact]
    public async Task AddPart_RejectsRepairRequestWithInvoice()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.Invoices.Add(new Invoice
        {
            RepairRequestId = database.RepairRequest.Id,
            InvoiceNumber = $"TEST-{Guid.NewGuid():N}",
            TotalAmount = 10m,
            IssuedById = database.Receptionist.Id
        });
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => database.CreateRepairRequestPartService().AddAsync(
            database.RepairRequest.Id,
            new RepairRequestPartCreateDto(database.Part.Id, 1),
            database.Technician.Id,
            UserRole.Technician));

        Assert.Equal(5, await database.GetStockAsync());
        Assert.Empty(await database.Context.RepairRequestParts.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task DeleteCategory_RejectsCategoryThatHasParts()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            database.CreateCategoryService().DeleteAsync(database.Category.Id));
    }

    [Fact]
    public async Task DeletePart_RejectsPartAlreadyUsed()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.RepairRequestParts.Add(new RepairRequestPart
        {
            RepairRequestId = database.RepairRequest.Id,
            PartId = database.Part.Id,
            Quantity = 1,
            UnitPrice = database.Part.UnitPrice
        });
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            database.CreatePartService().DeleteAsync(database.Part.Id));
    }

    [Fact]
    public async Task CreateCategory_RejectsDuplicateName()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            database.CreateCategoryService().CreateAsync(new CategoryCreateDto("Device Parts", null)));
    }

    [Fact]
    public async Task CreatePart_RejectsDuplicatePartNumber()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            database.CreatePartService().CreateAsync(new PartCreateDto(
                database.Category.Id, "Another Part", "PART-001", null, 1m, 1)));
    }

    [Fact]
    public async Task CreateAndUpdatePart_UseExistingCategory()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreatePartService();

        var created = await service.CreateAsync(new PartCreateDto(
            database.Category.Id, "New Part", "PART-002", null, 9.99m, 4));
        var updated = await service.UpdateAsync(created.Id, new PartUpdateDto(
            database.Category.Id, "Updated Part", "PART-002", null, 10.99m, 3));

        Assert.Equal("Device Parts", created.CategoryName);
        Assert.Equal("Updated Part", updated.Name);
        Assert.Equal("Device Parts", updated.CategoryName);
        Assert.Equal(3, updated.StockQuantity);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(SqliteConnection connection, AppDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public AppDbContext Context { get; }
        public Category Category { get; private set; } = null!;
        public Part Part { get; private set; } = null!;
        public User Technician { get; private set; } = null!;
        public User Receptionist { get; private set; } = null!;
        public RepairRequest RepairRequest { get; private set; } = null!;

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var database = new TestDatabase(connection, context);
            await database.SeedAsync();
            return database;
        }

        public CategoryService CreateCategoryService() =>
            new(new CategoryRepository(Context));

        public PartService CreatePartService() =>
            new(new PartRepository(Context), new CategoryRepository(Context));

        public RepairRequestPartService CreateRepairRequestPartService() =>
            new(
                new RepairRequestPartRepository(Context),
                new RepairRequestRepository(Context),
                new PartRepository(Context),
                new InvoiceRepository(Context));

        public Task<int> GetStockAsync() =>
            Context.Parts.AsNoTracking()
                .Where(part => part.Id == Part.Id)
                .Select(part => part.StockQuantity)
                .SingleAsync();

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private async Task SeedAsync()
        {
            Category = new Category { Name = "Device Parts" };
            Receptionist = new User
            {
                FullName = "Receptionist",
                Email = "receptionist@example.com",
                PasswordHash = "test-hash",
                Role = UserRole.Receptionist
            };
            Technician = new User
            {
                FullName = "Assigned Technician",
                Email = "technician@example.com",
                PasswordHash = "test-hash",
                Role = UserRole.Technician
            };
            var customer = new User
            {
                FullName = "Customer",
                Email = "customer@example.com",
                PasswordHash = "test-hash",
                Role = UserRole.Customer
            };
            var product = new Product
            {
                Name = "Test Product",
                Brand = "Example",
                Model = "Model A",
                SerialNumber = Guid.NewGuid().ToString("N"),
                WarrantyMonths = 12
            };
            var card = new WarrantyCard
            {
                Product = product,
                Customer = customer,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(12),
                Status = WarrantyStatus.Active
            };
            Part = new Part
            {
                Category = Category,
                PartNumber = "PART-001",
                Name = "Replacement Part",
                UnitPrice = 12.50m,
                StockQuantity = 5
            };
            RepairRequest = new RepairRequest
            {
                WarrantyCard = card,
                Technician = Technician,
                Description = "Test repair",
                Status = RepairRequestStatus.InProgress
            };

            Context.Users.Add(Receptionist);
            Context.Parts.Add(Part);
            Context.RepairRequests.Add(RepairRequest);
            await Context.SaveChangesAsync();
        }
    }
}
