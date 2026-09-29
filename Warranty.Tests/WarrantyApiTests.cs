using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Warranty.Controllers;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Services;

namespace Warranty.Tests;

public sealed class WarrantyApiTests
{
    [Fact]
    public async Task Register_AlwaysCreatesCustomerAndStoresOnlyPasswordHash()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var controller = new AuthController(testDatabase.Context, CreateTokenService());

        var result = await controller.Register(new RegisterRequest
        {
            FullName = "Customer One",
            Email = "PERSON@EXAMPLE.COM",
            Password = "StrongPassword123"
        }, CancellationToken.None);

        Assert.IsType<CreatedResult>(result.Result);
        var savedUser = await testDatabase.Context.Users.SingleAsync();
        Assert.Equal(UserRole.Customer, savedUser.Role);
        Assert.Equal("person@example.com", savedUser.Email);
        Assert.NotEqual("StrongPassword123", savedUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("StrongPassword123", savedUser.PasswordHash));
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
            Phone = "555-0100"
        }, CancellationToken.None);

        var savedUser = await testDatabase.Context.Users.SingleAsync();
        Assert.Equal("Updated Technician", savedUser.FullName);
        Assert.Equal("updated@example.com", savedUser.Email);
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
        var controller = WithUser(new RepairRequestsController(testDatabase.Context), customer.Id, UserRole.Customer);

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
        var controller = WithUser(new RepairRequestsController(testDatabase.Context), technician.Id, UserRole.Technician);

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
    public async Task ManagerCannotReadAllCustomerWarrantyCards()
    {
        await using var testDatabase = await TestDatabase.CreateAsync();
        var controller = WithUser(new WarrantyCardsController(testDatabase.Context), 1, UserRole.Manager);

        var result = await controller.GetAll(CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
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

    private static TokenService CreateTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "this-is-a-test-signing-key-at-least-32-bytes",
                ["Jwt:Issuer"] = "Warranty.Tests",
                ["Jwt:Audience"] = "Warranty.Tests"
            })
            .Build();
        return new TokenService(configuration);
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
