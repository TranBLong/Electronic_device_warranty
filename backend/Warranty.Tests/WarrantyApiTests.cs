using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Warranty.Controllers;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Options;
using Warranty.Services;

namespace Warranty.Tests;

public sealed class WarrantyApiTests
{
    [Fact]
    public async Task SeedAsync_InsertsSeedDataUsingGeneratedIds()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();

        await DatabaseSeeder.SeedAsync(testDatabase.Context);

        Assert.Equal(1000, await testDatabase.Context.Users.CountAsync());
        Assert.Equal(325, await testDatabase.Context.Products.CountAsync());
        Assert.Equal(28, await testDatabase.Context.RepairRequests.CountAsync());
        Assert.Equal(56, await testDatabase.Context.RepairRequestStatusHistories.CountAsync());
        Assert.Equal(5, await testDatabase.Context.RefreshTokens.CountAsync());
        Assert.All(await testDatabase.Context.Users.Select(user => user.Id).ToListAsync(), id => Assert.True(id > 0));
        Assert.All(await testDatabase.Context.Products.Select(product => product.Id).ToListAsync(), id => Assert.True(id > 0));

        var cards = await testDatabase.Context.WarrantyCards
            .Include(card => card.Product)
            .Include(card => card.Customer)
            .ToListAsync();
        Assert.All(cards, card => Assert.Equal(UserRole.Customer, card.Customer.Role));
        var coreSerialNumbers = new[]
        {
            "DLSXPS13-001", "IP15P-2024-001", "TABS9-2024-001", "LG34-2024-001", "CANON-G2020-001"
        };
        Assert.All(coreSerialNumbers,
            serialNumber => Assert.Contains(cards, card => card.Product.SerialNumber == serialNumber));

        await DatabaseSeeder.SeedAsync(testDatabase.Context);
        Assert.Equal(1000, await testDatabase.Context.Users.CountAsync());
    }

    [Fact]
    public async Task Register_AlwaysCreatesCustomerAndStoresOnlyPasswordHash()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var controller = new AuthController(testDatabase.Context, CreateTokenService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Register(new RegisterRequest
        {
            FullName = "Customer One",
            Email = "PERSON@EXAMPLE.COM",
            Password = "StrongPassword123",
            Address = "12 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh",
            DateOfBirth = new DateTime(1995, 3, 8),
            Gender = Gender.Female
        }, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedResult>(result.Result);
        var savedUser = await testDatabase.Context.Users.SingleAsync();
        Assert.Equal(UserRole.Customer, savedUser.Role);
        Assert.Equal("person@example.com", savedUser.Email);
        Assert.NotEqual("StrongPassword123", savedUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("StrongPassword123", savedUser.PasswordHash));
        Assert.Equal("12 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh", savedUser.Address);
        Assert.Equal(new DateTime(1995, 3, 8), savedUser.DateOfBirth);
        Assert.Equal(Gender.Female, savedUser.Gender);

        var response = Assert.IsType<UserResponse>(createdResult.Value);
        Assert.Equal(savedUser.Address, response.Address);
        Assert.Equal(savedUser.DateOfBirth, response.DateOfBirth);
        Assert.Equal(savedUser.Gender, response.Gender);
    }

    [Fact]
    public async Task Register_RejectsPasswordsOverBcryptUtf8Limit()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var controller = new AuthController(testDatabase.Context, CreateTokenService());

        var result = await controller.Register(new RegisterRequest
        {
            FullName = "Customer One",
            Email = "person@example.com",
            Password = new string('é', 40)
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(await testDatabase.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task UpdateMe_ChangesOnlyProfileFields()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var user = NewUser("tech@example.com", UserRole.Technician);
        testDatabase.Context.Users.Add(user);
        await testDatabase.Context.SaveChangesAsync();

        var controller = WithUser(new UsersController(testDatabase.Context), user.Id, UserRole.Technician);
        await controller.UpdateMe(new UpdateMyProfileRequest
        {
            FullName = "Updated Technician",
            Email = "updated@example.com",
            Phone = "555-0100",
            Address = "45 Lê Lợi, Quận 1, TP. Hồ Chí Minh",
            DateOfBirth = new DateTime(1990, 8, 20),
            Gender = Gender.Male
        }, CancellationToken.None);

        var savedUser = await testDatabase.Context.Users.SingleAsync();
        Assert.Equal("Updated Technician", savedUser.FullName);
        Assert.Equal("updated@example.com", savedUser.Email);
        Assert.Equal("45 Lê Lợi, Quận 1, TP. Hồ Chí Minh", savedUser.Address);
        Assert.Equal(new DateTime(1990, 8, 20), savedUser.DateOfBirth);
        Assert.Equal(Gender.Male, savedUser.Gender);
        Assert.Equal(UserRole.Technician, savedUser.Role);
        Assert.True(savedUser.IsActive);
    }

    [Fact]
    public async Task AdminCreateUser_RejectsMissingRoleInsteadOfDefaultingToAdmin()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var controller = new UsersController(testDatabase.Context);

        var result = await controller.Create(new AdminCreateUserRequest
        {
            FullName = "New User",
            Email = "new@example.com",
            Password = "StrongPassword123"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(await testDatabase.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task DeactivateMe_SoftDeletesAndRevokesRefreshTokensWithoutDeletingWarrantyData()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var customer = NewUser("customer@example.com", UserRole.Customer);
        var product = NewProduct();
        var card = new WarrantyCard
        {
            Product = product,
            Customer = customer,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(12)
        };
        testDatabase.Context.WarrantyCards.Add(card);
        testDatabase.Context.RefreshTokens.Add(new RefreshToken
        {
            User = customer,
            Token = "A1".PadRight(64, 'A'),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await testDatabase.Context.SaveChangesAsync();

        var controller = WithUser(new UsersController(testDatabase.Context), customer.Id, UserRole.Customer);
        var result = await controller.DeactivateMe(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.False((await testDatabase.Context.Users.SingleAsync()).IsActive);
        Assert.True((await testDatabase.Context.RefreshTokens.AsNoTracking().SingleAsync()).IsRevoked);
        Assert.Single(await testDatabase.Context.WarrantyCards.ToListAsync());
    }

    [Fact]
    public async Task Refresh_RotatesTokenAndRejectsReuseOfOldToken()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var user = NewUser("customer@example.com", UserRole.Customer);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123");
        testDatabase.Context.Users.Add(user);
        await testDatabase.Context.SaveChangesAsync();
        var controller = new AuthController(testDatabase.Context, CreateTokenService());

        var loginResult = await controller.Login(new LoginRequest
        {
            Email = user.Email,
            Password = "StrongPassword123"
        }, CancellationToken.None);
        var login = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(loginResult.Result).Value);

        var refreshResult = await controller.Refresh(
            new RefreshRequest { RefreshToken = login.RefreshToken },
            CancellationToken.None);
        var refreshed = Assert.IsType<AuthResponse>(Assert.IsType<OkObjectResult>(refreshResult.Result).Value);

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.True((await testDatabase.Context.RefreshTokens.OrderBy(token => token.Id).FirstAsync()).IsRevoked);

        var reuseResult = await controller.Refresh(
            new RefreshRequest { RefreshToken = login.RefreshToken },
            CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(reuseResult.Result);
    }

    [Fact]
    public async Task CustomerCreateRepairRequest_RequiresOwnActiveUnexpiredWarrantyAndLeavesReceptionistNull()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var customer = NewUser("customer@example.com", UserRole.Customer);
        var product = NewProduct();
        var card = new WarrantyCard
        {
            Product = product,
            Customer = customer,
            StartDate = DateTime.UtcNow.Date.AddMonths(-1),
            EndDate = DateTime.UtcNow.Date.AddMonths(11),
            Status = WarrantyStatus.Active
        };
        testDatabase.Context.WarrantyCards.Add(card);
        await testDatabase.Context.SaveChangesAsync();
        var controller = WithUser(
            new RepairRequestsController(
                testDatabase.Context,
                new RepairRequestService(testDatabase.Context),
                CreateAttachmentService(testDatabase.Context)),
            customer.Id, UserRole.Customer);

        var result = await controller.Create(new CreateRepairRequest
        {
            WarrantyCardId = card.Id,
            Description = "Device will not turn on"
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        var savedRequest = await testDatabase.Context.RepairRequests
            .Include(request => request.StatusHistory)
            .SingleAsync();
        Assert.Null(savedRequest.ReceptionistId);
        Assert.Equal(RepairRequestStatus.Received, savedRequest.Status);
        Assert.Single(savedRequest.StatusHistory);
        Assert.Null(savedRequest.StatusHistory.Single().OldStatus);

        var expiredCard = new WarrantyCard
        {
            ProductId = product.Id,
            CustomerId = customer.Id,
            StartDate = DateTime.UtcNow.Date.AddMonths(-13),
            EndDate = DateTime.UtcNow.Date.AddDays(-1),
            Status = WarrantyStatus.Active
        };
        testDatabase.Context.WarrantyCards.Add(expiredCard);
        await testDatabase.Context.SaveChangesAsync();
        var expiredResult = await controller.Create(new CreateRepairRequest
        {
            WarrantyCardId = expiredCard.Id,
            Description = "Expired warranty"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(expiredResult.Result);
        Assert.Single(await testDatabase.Context.RepairRequests.ToListAsync());
    }

    [Fact]
    public async Task UploadAttachment_RejectsFilesOverConfiguredSize()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var setup = await CreateAttachmentSetupAsync(testDatabase.Context, 1024);
        try
        {
            var file = CreateFormFile(
                new byte[1025], "large.jpg", "image/jpeg");

            var result = await setup.Controller.UploadAttachment(
                setup.Request.Id, file, CancellationToken.None);

            var problem = Assert.IsType<ProblemDetails>(
                Assert.IsType<BadRequestObjectResult>(result.Result).Value);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
            Assert.Empty(await testDatabase.Context.RepairRequestAttachments.ToListAsync());
        }
        finally
        {
            Directory.Delete(setup.ContentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAttachment_RejectsDisallowedExtension()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var setup = await CreateAttachmentSetupAsync(testDatabase.Context);
        try
        {
            var file = CreateFormFile(
                [0x25, 0x50, 0x44, 0x46, 0x2D], "document.exe", "application/pdf");

            var result = await setup.Controller.UploadAttachment(
                setup.Request.Id, file, CancellationToken.None);

            var problem = Assert.IsType<ProblemDetails>(
                Assert.IsType<BadRequestObjectResult>(result.Result).Value);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
            Assert.Empty(await testDatabase.Context.RepairRequestAttachments.ToListAsync());
        }
        finally
        {
            Directory.Delete(setup.ContentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAttachment_RejectsJpegWithInvalidMagicBytes()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var setup = await CreateAttachmentSetupAsync(testDatabase.Context);
        try
        {
            var file = CreateFormFile(
                Encoding.UTF8.GetBytes("not a jpeg"), "photo.jpg", "image/jpeg");

            var result = await setup.Controller.UploadAttachment(
                setup.Request.Id, file, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(await testDatabase.Context.RepairRequestAttachments.ToListAsync());
        }
        finally
        {
            Directory.Delete(setup.ContentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAttachment_SavesValidJpegAndSupportsListingAndDownload()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var setup = await CreateAttachmentSetupAsync(testDatabase.Context);
        try
        {
            var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0x00, 0x01 };
            var result = await setup.Controller.UploadAttachment(
                setup.Request.Id,
                CreateFormFile(bytes, "photo.jpg", "image/jpeg"),
                CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<RepairRequestAttachmentResponse>(created.Value);
            Assert.Equal("photo.jpg", response.FileName);
            Assert.Equal("image/jpeg", response.ContentType);
            Assert.Equal(bytes.Length, response.FileSize);

            var listResult = await setup.Controller.GetAttachments(
                setup.Request.Id, CancellationToken.None);
            var listed = Assert.Single(Assert.IsType<List<RepairRequestAttachmentResponse>>(
                Assert.IsType<OkObjectResult>(listResult.Result).Value));
            Assert.Equal(response.Id, listed.Id);

            var downloadResult = await setup.Controller.DownloadAttachment(
                setup.Request.Id, response.Id, CancellationToken.None);
            var physicalFile = Assert.IsType<PhysicalFileResult>(downloadResult);
            Assert.Equal("image/jpeg", physicalFile.ContentType);
            Assert.Equal(bytes, await File.ReadAllBytesAsync(physicalFile.FileName));
        }
        finally
        {
            Directory.Delete(setup.ContentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task TechnicianStatusChange_RequiresAssignmentAndWritesHistory()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var customer = NewUser("customer@example.com", UserRole.Customer);
        var technician = NewUser("tech@example.com", UserRole.Technician);
        var product = NewProduct();
        var card = new WarrantyCard
        {
            Product = product,
            Customer = customer,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(12)
        };
        var repairRequest = new RepairRequest
        {
            WarrantyCard = card,
            Technician = technician,
            Description = "Replace battery",
            Status = RepairRequestStatus.Received
        };
        testDatabase.Context.RepairRequests.Add(repairRequest);
        await testDatabase.Context.SaveChangesAsync();
        var controller = WithUser(
            new RepairRequestsController(
                testDatabase.Context,
                new RepairRequestService(testDatabase.Context),
                CreateAttachmentService(testDatabase.Context)),
            technician.Id, UserRole.Technician);

        var result = await controller.ChangeStatus(
            repairRequest.Id,
            new ChangeRepairStatusRequest { Status = RepairRequestStatus.InProgress },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        var savedRequest = await testDatabase.Context.RepairRequests
            .Include(request => request.StatusHistory)
            .SingleAsync();
        Assert.Equal(RepairRequestStatus.InProgress, savedRequest.Status);
        var history = Assert.Single(savedRequest.StatusHistory);
        Assert.Equal(RepairRequestStatus.Received, history.OldStatus);
        Assert.Equal(RepairRequestStatus.InProgress, history.NewStatus);
        Assert.Equal(technician.Id, history.ChangedBy);

        var prematureReturn = await controller.ChangeStatus(
            repairRequest.Id,
            new ChangeRepairStatusRequest { Status = RepairRequestStatus.Returned },
            CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(prematureReturn.Result);
        Assert.Equal(RepairRequestStatus.InProgress,
            (await testDatabase.Context.RepairRequests.AsNoTracking().SingleAsync()).Status);

        await controller.ChangeStatus(
            repairRequest.Id,
            new ChangeRepairStatusRequest { Status = RepairRequestStatus.Completed },
            CancellationToken.None);
        var returned = await controller.ChangeStatus(
            repairRequest.Id,
            new ChangeRepairStatusRequest { Status = RepairRequestStatus.Returned },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(returned.Result);
        var updatedRequest = await testDatabase.Context.RepairRequests
            .AsNoTracking()
            .Include(request => request.StatusHistory)
            .SingleAsync();
        Assert.Equal(RepairRequestStatus.Returned, updatedRequest.Status);
        Assert.Equal(3, updatedRequest.StatusHistory.Count);
    }

    [Fact]
    public async Task AssignTechnician_UpdatesRequestThroughService()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var manager = NewUser("manager@example.com", UserRole.Manager);
        var technician = NewUser("tech@example.com", UserRole.Technician);
        var repairRequest = new RepairRequest
        {
            WarrantyCard = new WarrantyCard
            {
                Product = NewProduct(),
                Customer = NewUser("customer@example.com", UserRole.Customer),
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(12)
            },
            Description = "Assign a technician"
        };
        testDatabase.Context.Users.AddRange(manager, technician);
        testDatabase.Context.RepairRequests.Add(repairRequest);
        await testDatabase.Context.SaveChangesAsync();
        var controller = WithUser(
            new RepairRequestsController(
                testDatabase.Context,
                new RepairRequestService(testDatabase.Context),
                CreateAttachmentService(testDatabase.Context)),
            manager.Id, UserRole.Manager);

        var result = await controller.AssignTechnician(
            repairRequest.Id,
            new AssignTechnicianRequest { TechnicianId = technician.Id },
            CancellationToken.None);

        var response = Assert.IsType<RepairRequestResponse>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(technician.Id, response.TechnicianId);
        Assert.Equal(technician.Id,
            (await testDatabase.Context.RepairRequests.SingleAsync()).TechnicianId);
    }

    [Fact]
    public async Task ReceptionistChangeStatus_IsForbidden()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var receptionist = NewUser("reception@example.com", UserRole.Receptionist);
        var customer = NewUser("customer@example.com", UserRole.Customer);
        var card = new WarrantyCard
        {
            Product = NewProduct(),
            Customer = customer,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(12)
        };
        var repairRequest = new RepairRequest
        {
            WarrantyCard = card,
            Description = "Check device"
        };
        testDatabase.Context.RepairRequests.Add(repairRequest);
        testDatabase.Context.Users.Add(receptionist);
        await testDatabase.Context.SaveChangesAsync();
        var controller = WithUser(
            new RepairRequestsController(
                testDatabase.Context,
                new RepairRequestService(testDatabase.Context),
                CreateAttachmentService(testDatabase.Context)),
            receptionist.Id, UserRole.Receptionist);

        var result = await controller.ChangeStatus(
            repairRequest.Id,
            new ChangeRepairStatusRequest { Status = RepairRequestStatus.InProgress },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Equal(RepairRequestStatus.Received,
            (await testDatabase.Context.RepairRequests.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task TechnicianCanOnlyViewAssignedRepairRequests()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var technician = NewUser("tech@example.com", UserRole.Technician);
        var customer = NewUser("customer@example.com", UserRole.Customer);
        var card = new WarrantyCard
        {
            Product = NewProduct(),
            Customer = customer,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(12)
        };
        var assignedRequest = new RepairRequest
        {
            WarrantyCard = card,
            Technician = technician,
            Description = "Assigned repair"
        };
        var unassignedRequest = new RepairRequest
        {
            WarrantyCard = card,
            Description = "Unassigned repair"
        };
        testDatabase.Context.RepairRequests.AddRange(assignedRequest, unassignedRequest);
        await testDatabase.Context.SaveChangesAsync();
        var controller = WithUser(
            new RepairRequestsController(
                testDatabase.Context,
                new RepairRequestService(testDatabase.Context),
                CreateAttachmentService(testDatabase.Context)),
            technician.Id, UserRole.Technician);

        var listResult = await controller.GetAll(
            new QueryParameters(), null, CancellationToken.None);
        var response = Assert.IsType<PagedResult<RepairRequestResponse>>(
            Assert.IsType<OkObjectResult>(listResult.Result).Value);
        var visibleRequest = Assert.Single(response.Items);
        Assert.Equal(assignedRequest.Id, visibleRequest.Id);

        var assignedResult = await controller.GetById(assignedRequest.Id, CancellationToken.None);
        Assert.IsType<OkObjectResult>(assignedResult.Result);
        var unassignedResult = await controller.GetById(unassignedRequest.Id, CancellationToken.None);
        Assert.IsType<ForbidResult>(unassignedResult.Result);
    }

    [Fact]
    public async Task AddPart_AccumulatesExistingLineAndChecksAdditionalStock()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var technician = NewUser("tech@example.com", UserRole.Technician);
        var customer = NewUser("customer@example.com", UserRole.Customer);
        var card = new WarrantyCard
        {
            Product = NewProduct(),
            Customer = customer,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(12)
        };
        var repairRequest = new RepairRequest
        {
            WarrantyCard = card,
            Technician = technician,
            Description = "Replace part"
        };
        var part = new Part
        {
            Code = "PART-1",
            Name = "Replacement part",
            UnitPrice = 125m,
            StockQuantity = 5
        };
        testDatabase.Context.RepairRequests.Add(repairRequest);
        testDatabase.Context.Parts.Add(part);
        await testDatabase.Context.SaveChangesAsync();
        var controller = WithUser(
            new RepairRequestsController(
                testDatabase.Context,
                new RepairRequestService(testDatabase.Context),
                CreateAttachmentService(testDatabase.Context)),
            technician.Id, UserRole.Technician);

        var firstResult = await controller.AddPart(
            repairRequest.Id,
            new AddRepairPartRequest { PartId = part.Id, Quantity = 2 },
            CancellationToken.None);
        var firstLine = Assert.IsType<RepairPartLineResponse>(
            Assert.IsType<OkObjectResult>(firstResult.Result).Value);

        var secondResult = await controller.AddPart(
            repairRequest.Id,
            new AddRepairPartRequest { PartId = part.Id, Quantity = 2 },
            CancellationToken.None);
        var secondLine = Assert.IsType<RepairPartLineResponse>(
            Assert.IsType<OkObjectResult>(secondResult.Result).Value);

        Assert.Equal(firstLine.Id, secondLine.Id);
        Assert.Equal(4, secondLine.Quantity);
        Assert.Equal(500m, secondLine.LineTotal);
        Assert.Equal(1, await testDatabase.Context.RepairRequestParts.CountAsync());
        Assert.Equal(1, (await testDatabase.Context.Parts.SingleAsync()).StockQuantity);

        var insufficientStockResult = await controller.AddPart(
            repairRequest.Id,
            new AddRepairPartRequest { PartId = part.Id, Quantity = 2 },
            CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(insufficientStockResult.Result);
        Assert.Equal(4, (await testDatabase.Context.RepairRequestParts.SingleAsync()).Quantity);
        Assert.Equal(1, (await testDatabase.Context.Parts.SingleAsync()).StockQuantity);
    }

    [Fact]
    public async Task LookupBySerial_IsAnonymousAndOmitsCustomerName()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var customer = NewUser("customer@example.com", UserRole.Customer);
        var product = NewProduct();
        product.SerialNumber = product.SerialNumber.ToUpperInvariant();
        var card = new WarrantyCard
        {
            Product = product,
            Customer = customer,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(12)
        };
        testDatabase.Context.WarrantyCards.Add(card);
        await testDatabase.Context.SaveChangesAsync();
        var controller = new WarrantyCardsController(
            testDatabase.Context,
            new WarrantyCardService(testDatabase.Context));

        var result = await controller.LookupBySerial(product.SerialNumber.ToLowerInvariant(), CancellationToken.None);

        var response = Assert.IsType<WarrantyCardLookupResponse>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(product.SerialNumber, response.SerialNumber);
        Assert.Null(typeof(WarrantyCardLookupResponse).GetProperty("CustomerName"));
        Assert.Contains(
            typeof(WarrantyCardsController)
                .GetMethod(nameof(WarrantyCardsController.LookupBySerial))!
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true),
            attribute => attribute is AllowAnonymousAttribute);
    }

    [Fact]
    public async Task WarrantyCardController_CreatesAndUpdatesThroughService()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var customer = NewUser("customer@example.com", UserRole.Customer);
        var product = NewProduct();
        testDatabase.Context.Users.Add(customer);
        testDatabase.Context.Products.Add(product);
        await testDatabase.Context.SaveChangesAsync();
        var controller = new WarrantyCardsController(
            testDatabase.Context,
            new WarrantyCardService(testDatabase.Context));
        var startDate = DateTime.UtcNow.Date.AddDays(-10);

        var createResult = await controller.Create(new CreateWarrantyCardRequest
        {
            ProductId = product.Id,
            CustomerId = customer.Id,
            StartDate = startDate
        }, CancellationToken.None);

        var created = Assert.IsType<WarrantyCardResponse>(
            Assert.IsType<CreatedAtActionResult>(createResult.Result).Value);
        Assert.Equal(startDate, created.StartDate);
        Assert.Equal(startDate.AddMonths(product.WarrantyMonths), created.EndDate);
        Assert.Equal(WarrantyStatus.Active, created.Status);

        var updatedStartDate = startDate.AddDays(1);
        var updateResult = await controller.Update(created.Id, new UpdateWarrantyCardRequest
        {
            StartDate = updatedStartDate,
            Status = WarrantyStatus.Voided
        }, CancellationToken.None);

        var updated = Assert.IsType<WarrantyCardResponse>(
            Assert.IsType<OkObjectResult>(updateResult.Result).Value);
        Assert.Equal(updatedStartDate, updated.StartDate);
        Assert.Equal(updatedStartDate.AddMonths(product.WarrantyMonths), updated.EndDate);
        Assert.Equal(WarrantyStatus.Voided, updated.Status);
    }

    [Fact]
    public async Task ManagerCanReadAllCustomerWarrantyCards_AndCustomerOnlySeesOwn()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var firstCustomer = NewUser("first@example.com", UserRole.Customer);
        var secondCustomer = NewUser("second@example.com", UserRole.Customer);
        var firstCard = new WarrantyCard
        {
            Product = NewProduct(),
            Customer = firstCustomer,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(12)
        };
        var secondCard = new WarrantyCard
        {
            Product = NewProduct(),
            Customer = secondCustomer,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(12)
        };
        testDatabase.Context.WarrantyCards.AddRange(firstCard, secondCard);
        await testDatabase.Context.SaveChangesAsync();

        var managerController = WithUser(
            new WarrantyCardsController(testDatabase.Context, new WarrantyCardService(testDatabase.Context)),
            1, UserRole.Manager);
        var managerResult = await managerController.GetAll(
            new QueryParameters(), null, CancellationToken.None);
        var managerCards = Assert.IsType<PagedResult<WarrantyCardResponse>>(
            Assert.IsType<OkObjectResult>(managerResult.Result).Value);
        Assert.Equal(2, managerCards.TotalCount);

        var customerController = WithUser(
            new WarrantyCardsController(testDatabase.Context, new WarrantyCardService(testDatabase.Context)),
            firstCustomer.Id, UserRole.Customer);
        var customerResult = await customerController.GetAll(
            new QueryParameters(), null, CancellationToken.None);
        var customerCards = Assert.IsType<PagedResult<WarrantyCardResponse>>(
            Assert.IsType<OkObjectResult>(customerResult.Result).Value);
        var ownCard = Assert.Single(customerCards.Items);
        Assert.Equal(firstCard.Id, ownCard.Id);
    }

    [Fact]
    public void AccessToken_ContainsRequiredClaimsAndExpiresAfterThirtyMinutes()
    {
        var tokenService = CreateTokenService();
        var user = NewUser("person@example.com", UserRole.Manager);

        var created = tokenService.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(created.Token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(nameof(UserRole.Manager), jwt.Claims.Single(claim => claim.Type == "role").Value);
        Assert.Equal(user.Email, jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.InRange((created.ExpiresAt - DateTime.UtcNow).TotalMinutes, 29, 30);
    }

    private static User NewUser(string email, UserRole role) => new()
    {
        FullName = "Test User",
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123"),
        Role = role
    };

    private static Product NewProduct() => new()
    {
        Name = "Laptop",
        Brand = "Example",
        Model = "X1",
        SerialNumber = Guid.NewGuid().ToString("N"),
        WarrantyMonths = 12
    };

    private static async Task<(RepairRequest Request, RepairRequestsController Controller, string ContentRoot)>
        CreateAttachmentSetupAsync(AppDbContext dbContext, long maxFileSizeBytes = 10 * 1024 * 1024)
    {
        var customer = NewUser($"customer-{Guid.NewGuid():N}@example.com", UserRole.Customer);
        var request = new RepairRequest
        {
            WarrantyCard = new WarrantyCard
            {
                Product = NewProduct(),
                Customer = customer,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(12)
            },
            Description = "Attachment test"
        };
        dbContext.RepairRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var contentRoot = Path.Combine(Path.GetTempPath(), $"Warranty.Tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRoot);
        var service = CreateAttachmentService(dbContext, contentRoot, maxFileSizeBytes);
        var controller = WithUser(
            new RepairRequestsController(dbContext, new RepairRequestService(dbContext), service),
            customer.Id, UserRole.Customer);
        return (request, controller, contentRoot);
    }

    private static RepairRequestAttachmentService CreateAttachmentService(
        AppDbContext dbContext,
        string? contentRoot = null,
        long maxFileSizeBytes = 10 * 1024 * 1024)
    {
        contentRoot ??= Path.GetTempPath();
        var options = new UploadOptions
        {
            RootPath = "uploads",
            MaxFileSizeBytes = maxFileSizeBytes
        };
        return new RepairRequestAttachmentService(
            dbContext,
            Microsoft.Extensions.Options.Options.Create(options),
            new TestWebHostEnvironment(contentRoot),
            NullLogger<RepairRequestAttachmentService>.Instance);
    }

    private static IFormFile CreateFormFile(byte[] bytes, string fileName, string contentType)
    {
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        return file;
    }

    private sealed class TestWebHostEnvironment(string contentRoot) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = nameof(WarrantyApiTests);
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = contentRoot;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static TokenService CreateTokenService()
    {
        return new TokenService(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            SigningKey = "this-is-a-test-signing-key-at-least-32-bytes",
            Issuer = "Warranty.Tests",
            Audience = "Warranty.Tests"
        }));
    }

    private static TController WithUser<TController>(TController controller, int userId, UserRole role)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim("role", role.ToString())
                ], "Test", ClaimTypes.Name, "role"))
            }
        };
        return controller;
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

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
