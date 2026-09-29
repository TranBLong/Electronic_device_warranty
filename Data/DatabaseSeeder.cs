using Microsoft.EntityFrameworkCore;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var users = new[]
        {
            new User
            {
                FullName = "Nguyen Van An",
                Email = "admin@warranty.local",
                PasswordHash = "$2a$11$y70l04nWlRrCUWpAxVJx1u6M32Gb35kPgBoYvzbOyGAz6wIhGJ2UW",
                Phone = "0901000001",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = new DateTime(2024, 01, 15, 08, 30, 00, DateTimeKind.Utc)
            },
            new User
            {
                FullName = "Tran Thi Binh",
                Email = "manager@warranty.local",
                PasswordHash = "$2a$11$sLiGaZHJ7MqHPuU1D5g2OuPRnkQ1lPp5FpHY5sh2agsJ7Gxspee0m",
                Phone = "0901000002",
                Role = UserRole.Manager,
                IsActive = true,
                CreatedAt = new DateTime(2024, 01, 20, 09, 00, 00, DateTimeKind.Utc)
            },
            new User
            {
                FullName = "Le Hoai Chi",
                Email = "reception@warranty.local",
                PasswordHash = "$2a$11$ukvy3qgFwz3vJz9Ieo4YG.VH1SWFcZx2B5EHQRXb0S7UlM34tZr/m",
                Phone = "0901000003",
                Role = UserRole.Receptionist,
                IsActive = true,
                CreatedAt = new DateTime(2024, 02, 03, 08, 15, 00, DateTimeKind.Utc)
            },
            new User
            {
                FullName = "Pham Van Duy",
                Email = "tech01@warranty.local",
                PasswordHash = "$2a$11$w/FH/Jd561N7aKbsTv4WBuemHHvejOdTcgLC9BisMu.waSjKXwr6.",
                Phone = "0901000004",
                Role = UserRole.Technician,
                IsActive = true,
                CreatedAt = new DateTime(2024, 02, 10, 07, 45, 00, DateTimeKind.Utc)
            },
            new User
            {
                FullName = "Nguyen Thi Emy",
                Email = "customer01@warranty.local",
                PasswordHash = "$2a$11$ojyUBHqBKEeQNyAOFogOMu5bLNHgRbsGLl02AGP/OBCrKwiT6zAdq",
                Phone = "0901000005",
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = new DateTime(2024, 03, 08, 14, 20, 00, DateTimeKind.Utc)
            }
        };

        dbContext.Users.AddRange(users);

        var products = new[]
        {
            new Product
            {
                Name = "Dell XPS 13",
                Brand = "Dell",
                Model = "XPS 13 9310",
                SerialNumber = "DLSXPS13-001",
                WarrantyMonths = 24,
                CreatedAt = new DateTime(2024, 02, 05, 10, 00, 00, DateTimeKind.Utc)
            },
            new Product
            {
                Name = "iPhone 15 Pro",
                Brand = "Apple",
                Model = "A3102",
                SerialNumber = "IP15P-2024-001",
                WarrantyMonths = 12,
                CreatedAt = new DateTime(2024, 02, 12, 11, 35, 00, DateTimeKind.Utc)
            },
            new Product
            {
                Name = "Samsung Galaxy Tab S9",
                Brand = "Samsung",
                Model = "SM-X710",
                SerialNumber = "TABS9-2024-001",
                WarrantyMonths = 18,
                CreatedAt = new DateTime(2024, 03, 01, 09, 10, 00, DateTimeKind.Utc)
            },
            new Product
            {
                Name = "LG UltraWide 34\" Monitor",
                Brand = "LG",
                Model = "34WN80C-B",
                SerialNumber = "LG34-2024-001",
                WarrantyMonths = 24,
                CreatedAt = new DateTime(2024, 03, 15, 15, 25, 00, DateTimeKind.Utc)
            },
            new Product
            {
                Name = "Canon Pixma G2020",
                Brand = "Canon",
                Model = "G2020",
                SerialNumber = "CANON-G2020-001",
                WarrantyMonths = 12,
                CreatedAt = new DateTime(2024, 04, 08, 13, 40, 00, DateTimeKind.Utc)
            }
        };

        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync();

        var warrantyCards = new[]
        {
            new WarrantyCard
            {
                ProductId = products[0].Id,
                CustomerId = users[4].Id,
                StartDate = new DateTime(2024, 01, 15, 00, 00, 00, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 01, 15, 00, 00, 00, DateTimeKind.Utc),
                Status = WarrantyStatus.Active
            },
            new WarrantyCard
            {
                ProductId = products[1].Id,
                CustomerId = users[4].Id,
                StartDate = new DateTime(2024, 03, 01, 00, 00, 00, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 03, 01, 00, 00, 00, DateTimeKind.Utc),
                Status = WarrantyStatus.Active
            },
            new WarrantyCard
            {
                ProductId = products[2].Id,
                CustomerId = users[4].Id,
                StartDate = new DateTime(2023, 06, 10, 00, 00, 00, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 06, 10, 00, 00, 00, DateTimeKind.Utc),
                Status = WarrantyStatus.Expired
            },
            new WarrantyCard
            {
                ProductId = products[3].Id,
                CustomerId = users[4].Id,
                StartDate = new DateTime(2024, 11, 20, 00, 00, 00, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 11, 20, 00, 00, 00, DateTimeKind.Utc),
                Status = WarrantyStatus.Active
            },
            new WarrantyCard
            {
                ProductId = products[4].Id,
                CustomerId = users[4].Id,
                StartDate = new DateTime(2022, 05, 25, 00, 00, 00, DateTimeKind.Utc),
                EndDate = new DateTime(2023, 05, 25, 00, 00, 00, DateTimeKind.Utc),
                Status = WarrantyStatus.Voided
            }
        };

        dbContext.WarrantyCards.AddRange(warrantyCards);
        await dbContext.SaveChangesAsync();

        var repairRequests = new[]
        {
            new RepairRequest
            {
                WarrantyCardId = warrantyCards[0].Id,
                ReceptionistId = users[2].Id,
                TechnicianId = users[3].Id,
                Description = "Laptop battery drains quickly and device turns off during meetings.",
                Status = RepairRequestStatus.Received,
                CreatedAt = new DateTime(2025, 01, 20, 08, 40, 00, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 01, 20, 08, 40, 00, DateTimeKind.Utc)
            },
            new RepairRequest
            {
                WarrantyCardId = warrantyCards[1].Id,
                ReceptionistId = users[2].Id,
                TechnicianId = users[3].Id,
                Description = "Touchscreen is unresponsive after a minor drop.",
                Status = RepairRequestStatus.InProgress,
                CreatedAt = new DateTime(2025, 02, 02, 10, 00, 00, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 02, 05, 09, 15, 00, DateTimeKind.Utc)
            },
            new RepairRequest
            {
                WarrantyCardId = warrantyCards[2].Id,
                ReceptionistId = users[2].Id,
                TechnicianId = users[3].Id,
                Description = "Tablet charging port is loose and power cable disconnects intermittently.",
                Status = RepairRequestStatus.Completed,
                CreatedAt = new DateTime(2024, 07, 05, 14, 20, 00, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 07, 11, 16, 10, 00, DateTimeKind.Utc)
            },
            new RepairRequest
            {
                WarrantyCardId = warrantyCards[3].Id,
                ReceptionistId = users[2].Id,
                TechnicianId = users[3].Id,
                Description = "Monitor flickers black every few minutes while using design software.",
                Status = RepairRequestStatus.Returned,
                CreatedAt = new DateTime(2025, 04, 12, 12, 00, 00, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 04, 18, 17, 30, 00, DateTimeKind.Utc)
            },
            new RepairRequest
            {
                WarrantyCardId = warrantyCards[4].Id,
                ReceptionistId = users[2].Id,
                TechnicianId = users[3].Id,
                Description = "Printer repeatedly jams paper after installing a new cartridge.",
                Status = RepairRequestStatus.Cancelled,
                CreatedAt = new DateTime(2023, 04, 25, 09, 35, 00, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2023, 04, 28, 13, 45, 00, DateTimeKind.Utc)
            }
        };

        dbContext.RepairRequests.AddRange(repairRequests);
        await dbContext.SaveChangesAsync();

        var histories = new[]
        {
            new RepairRequestStatusHistory
            {
                RepairRequestId = repairRequests[0].Id,
                OldStatus = null,
                NewStatus = RepairRequestStatus.Received,
                ChangedBy = users[2].Id,
                ChangedAt = new DateTime(2025, 01, 20, 08, 41, 00, DateTimeKind.Utc)
            },
            new RepairRequestStatusHistory
            {
                RepairRequestId = repairRequests[1].Id,
                OldStatus = RepairRequestStatus.Received,
                NewStatus = RepairRequestStatus.InProgress,
                ChangedBy = users[3].Id,
                ChangedAt = new DateTime(2025, 02, 05, 09, 15, 00, DateTimeKind.Utc)
            },
            new RepairRequestStatusHistory
            {
                RepairRequestId = repairRequests[2].Id,
                OldStatus = RepairRequestStatus.InProgress,
                NewStatus = RepairRequestStatus.Completed,
                ChangedBy = users[3].Id,
                ChangedAt = new DateTime(2024, 07, 11, 16, 10, 00, DateTimeKind.Utc)
            },
            new RepairRequestStatusHistory
            {
                RepairRequestId = repairRequests[3].Id,
                OldStatus = RepairRequestStatus.Completed,
                NewStatus = RepairRequestStatus.Returned,
                ChangedBy = users[3].Id,
                ChangedAt = new DateTime(2025, 04, 18, 17, 30, 00, DateTimeKind.Utc)
            },
            new RepairRequestStatusHistory
            {
                RepairRequestId = repairRequests[4].Id,
                OldStatus = RepairRequestStatus.Received,
                NewStatus = RepairRequestStatus.Cancelled,
                ChangedBy = users[2].Id,
                ChangedAt = new DateTime(2023, 04, 28, 13, 45, 00, DateTimeKind.Utc)
            }
        };

        dbContext.RepairRequestStatusHistories.AddRange(histories);
        await dbContext.SaveChangesAsync();

        var refreshTokens = new[]
        {
            new RefreshToken
            {
                UserId = users[0].Id,
                Token = GenerateRefreshTokenHash(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new RefreshToken
            {
                UserId = users[1].Id,
                Token = GenerateRefreshTokenHash(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new RefreshToken
            {
                UserId = users[2].Id,
                Token = GenerateRefreshTokenHash(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new RefreshToken
            {
                UserId = users[3].Id,
                Token = GenerateRefreshTokenHash(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new RefreshToken
            {
                UserId = users[4].Id,
                Token = GenerateRefreshTokenHash(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        dbContext.RefreshTokens.AddRange(refreshTokens);
        await dbContext.SaveChangesAsync();
    }

    private static string GenerateRefreshTokenHash() =>
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}
