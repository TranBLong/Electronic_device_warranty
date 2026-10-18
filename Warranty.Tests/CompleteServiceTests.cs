using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Exceptions;
using Warranty.Models;
using Warranty.Repositories.Implementations.Complete;
using Warranty.Repositories.Implementations.Important;
using Warranty.Repositories.Implementations.Worth;
using Warranty.Services.Implementations.Complete;
using Warranty.Services.Implementations.Important;

namespace Warranty.Tests;

public sealed class CompleteServiceTests
{
    [Theory]
    [InlineData(RepairRequestStatus.Completed)]
    [InlineData(RepairRequestStatus.Returned)]
    public async Task CreateInvoice_AllowsCompletedOrReturnedAndCalculatesPartTotal(RepairRequestStatus status)
    {
        await using var database = await TestDatabase.CreateAsync();
        database.RepairRequest.IsChargeable = true;
        await database.Context.SaveChangesAsync();
        await database.SetRepairRequestStatusAsync(status);

        var invoice = await database.CreateInvoiceService().CreateAsync(
            database.RepairRequest.Id,
            database.Receptionist.Id,
            UserRole.Receptionist);

        Assert.Equal(25m, invoice.TotalAmount);
        Assert.Equal(25m, invoice.PartsTotal);
        Assert.Equal(0m, invoice.LaborTotal);
        Assert.Single(invoice.Lines);
        Assert.Equal(25m, invoice.Lines[0].LineTotal);
        Assert.Equal(InvoiceStatus.Unpaid, invoice.Status);
    }

    [Fact]
    public async Task CreateInvoice_RejectsInProgressRepairRequest()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateInvoiceService().CreateAsync(
            database.RepairRequest.Id, database.Receptionist.Id, UserRole.Receptionist));
    }

    [Fact]
    public async Task CreateInvoice_RejectsDuplicate()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.SetRepairRequestStatusAsync(RepairRequestStatus.Completed);
        var service = database.CreateInvoiceService();
        await service.CreateAsync(database.RepairRequest.Id, database.Receptionist.Id, UserRole.Receptionist);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            database.RepairRequest.Id, database.Receptionist.Id, UserRole.Receptionist));
    }

    [Theory]
    [InlineData(true, 0, 25)]
    [InlineData(true, 40, 65)]
    [InlineData(false, 0, 0)]
    public async Task CreateInvoice_CalculatesFromPersistedChargeabilityAndLabor(
        bool isChargeable,
        decimal laborCost,
        decimal expectedTotal)
    {
        await using var database = await TestDatabase.CreateAsync();
        database.RepairRequest.IsChargeable = isChargeable;
        database.RepairRequest.LaborCost = laborCost;
        await database.Context.SaveChangesAsync();
        await database.SetRepairRequestStatusAsync(RepairRequestStatus.Completed);

        var invoice = await database.CreateInvoiceService().CreateAsync(
            database.RepairRequest.Id,
            database.Receptionist.Id,
            UserRole.Receptionist);

        Assert.Equal(25m, invoice.PartsTotal);
        Assert.Equal(laborCost, invoice.LaborTotal);
        Assert.Equal(expectedTotal, invoice.TotalAmount);
        Assert.Equal(expectedTotal == 0m ? InvoiceStatus.Paid : InvoiceStatus.Unpaid, invoice.Status);
        Assert.Equal(expectedTotal == 0m, invoice.PaidAt.HasValue);
        Assert.Equal(expectedTotal, invoice.BalanceDue);
    }

    [Fact]
    public async Task CreatePayment_PartialPaymentKeepsInvoiceUnpaid()
    {
        await using var database = await TestDatabase.CreateAsync();
        var invoice = await database.AddInvoiceAsync(100m);

        await database.CreatePaymentService().CreateAsync(
            invoice.Id, new PaymentCreateDto(40m, PaymentMethod.Cash, null),
            database.Receptionist.Id, UserRole.Receptionist);

        Assert.Equal(InvoiceStatus.Unpaid, (await database.GetInvoiceAsync(invoice.Id)).Status);
    }

    [Fact]
    public async Task CreatePayment_RejectsAmountAboveOutstandingBalance()
    {
        await using var database = await TestDatabase.CreateAsync();
        var invoice = await database.AddInvoiceAsync(100m);
        var service = database.CreatePaymentService();
        await service.CreateAsync(
            invoice.Id, new PaymentCreateDto(40m, PaymentMethod.Cash, null),
            database.Receptionist.Id, UserRole.Receptionist);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(
            invoice.Id, new PaymentCreateDto(61m, PaymentMethod.Cash, null),
            database.Receptionist.Id, UserRole.Receptionist));

        Assert.Single(await database.Context.Payments.AsNoTracking().ToListAsync());
        Assert.Equal(InvoiceStatus.Unpaid, (await database.GetInvoiceAsync(invoice.Id)).Status);
    }

    [Fact]
    public async Task CreatePayment_FullPaymentMarksInvoicePaid()
    {
        await using var database = await TestDatabase.CreateAsync();
        var invoice = await database.AddInvoiceAsync(100m);

        await database.CreatePaymentService().CreateAsync(
            invoice.Id, new PaymentCreateDto(100m, PaymentMethod.Transfer, null),
            database.Receptionist.Id, UserRole.Receptionist);

        var savedInvoice = await database.GetInvoiceAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.Paid, savedInvoice.Status);
        Assert.NotNull(savedInvoice.PaidAt);
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task CreatePayment_RejectsPaidOrCancelledInvoice(InvoiceStatus status)
    {
        await using var database = await TestDatabase.CreateAsync();
        var invoice = await database.AddInvoiceAsync(100m);
        invoice.Status = status;
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => database.CreatePaymentService().CreateAsync(
            invoice.Id, new PaymentCreateDto(10m, PaymentMethod.Cash, null),
            database.Receptionist.Id, UserRole.Receptionist));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreatePayment_RejectsNonPositiveAmount(decimal amount)
    {
        await using var database = await TestDatabase.CreateAsync();
        var invoice = await database.AddInvoiceAsync(100m);

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreatePaymentService().CreateAsync(
            invoice.Id, new PaymentCreateDto(amount, PaymentMethod.Cash, null),
            database.Receptionist.Id, UserRole.Receptionist));
    }

    [Fact]
    public async Task CreateFeedback_RejectsRepairRequestNotReturned()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateFeedbackService().CreateAsync(
            database.RepairRequest.Id, new FeedbackCreateDto(5, null),
            database.Customer.Id, UserRole.Customer));
    }

    [Fact]
    public async Task CreateFeedback_RejectsCustomerWhoDoesNotOwnRepairRequest()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.SetRepairRequestStatusAsync(RepairRequestStatus.Returned);

        await Assert.ThrowsAsync<ForbiddenException>(() => database.CreateFeedbackService().CreateAsync(
            database.RepairRequest.Id, new FeedbackCreateDto(5, null),
            database.OtherCustomer.Id, UserRole.Customer));
    }

    [Fact]
    public async Task CreateFeedback_RejectsSecondSubmission()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.SetRepairRequestStatusAsync(RepairRequestStatus.Returned);
        var service = database.CreateFeedbackService();
        await service.CreateAsync(database.RepairRequest.Id, new FeedbackCreateDto(4, "Good"),
            database.Customer.Id, UserRole.Customer);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            database.RepairRequest.Id, new FeedbackCreateDto(5, null),
            database.Customer.Id, UserRole.Customer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateFeedback_RejectsRatingOutsideRange(int rating)
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.SetRepairRequestStatusAsync(RepairRequestStatus.Returned);

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateFeedbackService().CreateAsync(
            database.RepairRequest.Id, new FeedbackCreateDto(rating, null),
            database.Customer.Id, UserRole.Customer));
    }

    [Fact]
    public async Task ChangeRepairStatus_CreatesOneNotificationForWarrantyOwner()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateRepairRequestService();

        await service.ChangeStatusAsync(
            database.RepairRequest.Id, RepairRequestStatus.Completed,
            database.Technician.Id, UserRole.Technician);

        var notifications = await database.Context.Notifications.AsNoTracking().ToListAsync();
        var notification = Assert.Single(notifications);
        Assert.Equal(database.Customer.Id, notification.UserId);
        Assert.Equal(database.RepairRequest.Id, notification.RepairRequestId);
        Assert.Contains("Completed", notification.Message);
        Assert.Single(await database.Context.RepairRequestStatusHistories.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task ChangeRepairStatus_RejectsTransitionsOutsideApprovedGraph()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.SetRepairRequestStatusAsync(RepairRequestStatus.Received);

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateRepairRequestService().ChangeStatusAsync(
            database.RepairRequest.Id, RepairRequestStatus.Completed,
            database.Technician.Id, UserRole.Technician));

        Assert.Equal(RepairRequestStatus.Received,
            (await database.Context.RepairRequests.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task ChangeRepairStatus_ReceptionistCanReturnCompletedRequest()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.SetRepairRequestStatusAsync(RepairRequestStatus.Completed);

        var updated = await database.CreateRepairRequestService().ChangeStatusAsync(
            database.RepairRequest.Id, RepairRequestStatus.Returned,
            database.Receptionist.Id, UserRole.Receptionist);

        Assert.Equal(RepairRequestStatus.Returned, updated.Status);
        var history = await database.Context.RepairRequestStatusHistories.AsNoTracking().SingleAsync();
        Assert.Equal(database.Receptionist.Id, history.ChangedBy);
    }

    [Fact]
    public async Task SetChargeability_ValidatesRoleStateAndLaborCost()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateRepairRequestService();

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.SetChargeabilityAsync(
            database.RepairRequest.Id, new RepairRequestChargeabilityDto(false, 5m),
            database.Technician.Id, UserRole.Technician));

        var response = await service.SetChargeabilityAsync(
            database.RepairRequest.Id, new RepairRequestChargeabilityDto(true, 50m),
            database.Technician.Id, UserRole.Technician);

        Assert.True(response.IsChargeable);
        Assert.Equal(50m, response.LaborCost);
        var persisted = await database.Context.RepairRequests.AsNoTracking().SingleAsync();
        Assert.True(persisted.IsChargeable);
        Assert.Equal(50m, persisted.LaborCost);
    }

    [Fact]
    public async Task SetChargeability_RejectsUnassignedTechnicianAndExistingInvoice()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.CreateRepairRequestService();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.SetChargeabilityAsync(
            database.RepairRequest.Id, new RepairRequestChargeabilityDto(true, 10m),
            database.OtherCustomer.Id, UserRole.Technician));

        await database.SetRepairRequestStatusAsync(RepairRequestStatus.Completed);
        await database.AddInvoiceAsync(10m);
        await Assert.ThrowsAsync<ConflictException>(() => service.SetChargeabilityAsync(
            database.RepairRequest.Id, new RepairRequestChargeabilityDto(true, 10m),
            database.Manager.Id, UserRole.Manager));
    }

    [Fact]
    public async Task MarkNotificationRead_RejectsOtherUser()
    {
        await using var database = await TestDatabase.CreateAsync();
        var notification = new Notification
        {
            UserId = database.Customer.Id,
            Title = "Repair updated",
            Message = "Updated"
        };
        database.Context.Notifications.Add(notification);
        await database.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => database.CreateNotificationService().MarkReadAsync(
            notification.Id, database.OtherCustomer.Id));
    }

    [Fact]
    public async Task UploadAttachment_RejectsUnsupportedExtension()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateAttachmentService().UploadAsync(
            database.RepairRequest.Id, MakeFile("malware.exe", "application/octet-stream", [0x4D, 0x5A]),
            database.Customer.Id, UserRole.Customer));
    }

    [Fact]
    public async Task UploadAttachment_RejectsMismatchedFileSignature()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateAttachmentService().UploadAsync(
            database.RepairRequest.Id, MakeFile("not-an-image.jpg", "image/jpeg", "not a jpeg"u8.ToArray()),
            database.Customer.Id, UserRole.Customer));
    }

    [Fact]
    public async Task UploadAttachment_RejectsFileAboveFiveMegabytes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var bytes = new byte[5 * 1024 * 1024 + 1];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateAttachmentService().UploadAsync(
            database.RepairRequest.Id, MakeFile("large.jpg", "image/jpeg", bytes),
            database.Customer.Id, UserRole.Customer));
    }

    [Fact]
    public async Task UploadAttachment_RejectsUserWithoutRepairRequestAccess()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => database.CreateAttachmentService().UploadAsync(
            database.RepairRequest.Id, MakeFile("valid.pdf", "application/pdf", "%PDF-"u8.ToArray()),
            database.OtherCustomer.Id, UserRole.Customer));
    }

    [Fact]
    public async Task UploadAttachment_StoresRandomRelativePathAndSanitizedName()
    {
        await using var database = await TestDatabase.CreateAsync();
        var content = "%PDF-1.7\n"u8.ToArray();

        var uploaded = await database.CreateAttachmentService().UploadAsync(
            database.RepairRequest.Id, MakeFile("../../private.pdf", "application/pdf", content),
            database.Customer.Id, UserRole.Customer);
        var download = await database.CreateAttachmentService().GetDownloadAsync(
            uploaded.Id, database.Customer.Id, UserRole.Customer);

        Assert.Equal("private.pdf", uploaded.FileName);
        Assert.DoesNotContain("private.pdf", download.FilePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(content, await File.ReadAllBytesAsync(download.FilePath));
    }

    [Fact]
    public async Task CreateServiceCenter_RejectsNonManagerManagerId()
    {
        await using var database = await TestDatabase.CreateAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => database.CreateServiceCenterService().CreateAsync(
            new ServiceCenterCreateDto("Test Center", "Test address", null, database.Customer.Id)));
    }

    private static FormFile MakeFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly string _uploadRoot = Path.Combine(Path.GetTempPath(), $"warranty-tests-{Guid.NewGuid():N}");

        private TestDatabase(SqliteConnection connection, AppDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public AppDbContext Context { get; }
        public User Manager { get; private set; } = null!;
        public User Receptionist { get; private set; } = null!;
        public User Technician { get; private set; } = null!;
        public User Customer { get; private set; } = null!;
        public User OtherCustomer { get; private set; } = null!;
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

        public InvoiceService CreateInvoiceService() => new(
            new InvoiceRepository(Context),
            new RepairRequestRepository(Context),
            new RepairRequestPartRepository(Context));

        public PaymentService CreatePaymentService() =>
            new(new PaymentRepository(Context), new InvoiceRepository(Context));

        public FeedbackService CreateFeedbackService() =>
            new(new FeedbackRepository(Context), new RepairRequestRepository(Context));

        public NotificationService CreateNotificationService() => new(new NotificationRepository(Context));

        public RepairRequestService CreateRepairRequestService() => new(
            new RepairRequestRepository(Context),
            new WarrantyCardRepository(Context),
            new UserRepository(Context),
            new ServiceCenterRepository(Context),
            new InvoiceRepository(Context));

        public ServiceCenterService CreateServiceCenterService() =>
            new(new ServiceCenterRepository(Context), new UserRepository(Context));

        public RepairRequestAttachmentService CreateAttachmentService()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Upload:MaxFileSizeBytes"] = (5 * 1024 * 1024).ToString(),
                    ["Upload:AllowedExtensions:0"] = ".jpg",
                    ["Upload:AllowedExtensions:1"] = ".jpeg",
                    ["Upload:AllowedExtensions:2"] = ".png",
                    ["Upload:AllowedExtensions:3"] = ".pdf",
                    ["Upload:RootPath"] = "uploads"
                })
                .Build();
            var environment = new TestWebHostEnvironment(_uploadRoot);
            return new RepairRequestAttachmentService(
                new RepairRequestAttachmentRepository(Context),
                new RepairRequestRepository(Context),
                configuration,
                environment);
        }

        public async Task SetRepairRequestStatusAsync(RepairRequestStatus status)
        {
            RepairRequest.Status = status;
            await Context.SaveChangesAsync();
        }

        public async Task<Invoice> AddInvoiceAsync(decimal total)
        {
            var invoice = new Invoice
            {
                RepairRequestId = RepairRequest.Id,
                InvoiceNumber = $"TEST-{Guid.NewGuid():N}",
                TotalAmount = total,
                PartsTotal = total,
                IssuedById = Receptionist.Id
            };
            Context.Invoices.Add(invoice);
            await Context.SaveChangesAsync();
            return invoice;
        }

        public Task<Invoice> GetInvoiceAsync(int id) =>
            Context.Invoices.AsNoTracking().SingleAsync(invoice => invoice.Id == id);

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
            if (Directory.Exists(_uploadRoot))
            {
                Directory.Delete(_uploadRoot, recursive: true);
            }
        }

        private async Task SeedAsync()
        {
            Manager = NewUser("manager@example.com", UserRole.Manager);
            Receptionist = NewUser("receptionist@example.com", UserRole.Receptionist);
            Technician = NewUser("technician@example.com", UserRole.Technician);
            Customer = NewUser("customer@example.com", UserRole.Customer);
            OtherCustomer = NewUser("other@example.com", UserRole.Customer);
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
                Customer = Customer,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(12)
            };
            RepairRequest = new RepairRequest
            {
                WarrantyCard = card,
                Technician = Technician,
                Description = "Test repair",
                Status = RepairRequestStatus.InProgress
            };
            var category = new Category { Name = "Test Category" };
            var part = new Part
            {
                Category = category,
                PartNumber = "TEST-PART",
                Name = "Test Part",
                UnitPrice = 10m,
                StockQuantity = 10
            };
            RepairRequest.Parts.Add(new RepairRequestPart
            {
                Part = part,
                Quantity = 2,
                UnitPrice = 12.50m
            });
            Context.Users.AddRange(Manager, Receptionist, Technician, Customer, OtherCustomer);
            Context.RepairRequests.Add(RepairRequest);
            await Context.SaveChangesAsync();
        }

        private static User NewUser(string email, UserRole role) => new()
        {
            FullName = role.ToString(),
            Email = email,
            PasswordHash = "test-hash",
            Role = role
        };
    }

    private sealed class TestWebHostEnvironment(string rootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Warranty.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = rootPath;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = rootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
