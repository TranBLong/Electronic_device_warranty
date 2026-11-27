using Microsoft.EntityFrameworkCore;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Services;

namespace Warranty.Data;

public static partial class DatabaseSeeder
{
    private static readonly string[] FirstNames =
    [
        "An","Binh","Chi","Dung","Em","Phong","Giang","Hai","Khanh","Linh",
        "Minh","Nam","Oanh","Phuc","Quang","Rang","Son","Tam","Uyen","Viet",
        "Xuan","Yen","Bao","Cuong","Diep","Hoa","Kiet","Lan","Mai","Nga"
    ];
    private static readonly string[] LastNames =
    [
        "Nguyen","Tran","Le","Pham","Hoang","Huynh","Phan","Vu","Vo","Dang",
        "Bui","Do","Ho","Ngo","Duong","Ly","Dinh","Truong","Tong","Cao"
    ];
    private static readonly string[] Brands =
        ["Apple","Samsung","Dell","HP","Lenovo","Asus","Xiaomi","Sony","LG","Acer"];
    private static readonly string[] ProductKinds =
        ["Laptop","Phone","Tablet","Monitor","Printer","Router","Headset","Keyboard"];

    /// <summary>
    /// Seeds ~1000 realistic users + products + warranty cards for pagination demos.
    /// Idempotent: only runs when total Users &lt; 50.
    /// </summary>
    public static async Task SeedBulkDemoAsync(AppDbContext db)
    {
        var userCount = await db.Users.CountAsync();
        if (userCount >= 100)
            return;

        var rng = new Random(2026);
        var passwordHash = PasswordHashing.Hash("Customer@123");
        var techHash = PasswordHashing.Hash("Technician@123");
        var now = DateTime.UtcNow;

        // --- bulk customers (~900) ---
        var customers = new List<User>(900);
        for (var i = 1; i <= 900; i++)
        {
            var fn = FirstNames[rng.Next(FirstNames.Length)];
            var ln = LastNames[rng.Next(LastNames.Length)];
            customers.Add(new User
            {
                FullName = $"{ln} {fn} {i:D3}",
                Email = $"customer{i:D4}@demo.warranty.local",
                PasswordHash = passwordHash,
                Phone = $"09{rng.Next(10000000, 99999999)}",
                Role = UserRole.Customer,
                IsActive = rng.NextDouble() > 0.05,
                CreatedAt = now.AddDays(-rng.Next(1, 700))
            });
        }

        // --- bulk technicians (~40) ---
        var technicians = new List<User>(40);
        for (var i = 1; i <= 40; i++)
        {
            var fn = FirstNames[rng.Next(FirstNames.Length)];
            var ln = LastNames[rng.Next(LastNames.Length)];
            technicians.Add(new User
            {
                FullName = $"KT {ln} {fn}",
                Email = $"tech{i:D3}@demo.warranty.local",
                PasswordHash = techHash,
                Phone = $"08{rng.Next(10000000, 99999999)}",
                Role = UserRole.Technician,
                IsActive = true,
                CreatedAt = now.AddDays(-rng.Next(30, 400))
            });
        }

        // --- receptionists (~20) + managers (~15) ---
        var staffExtra = new List<User>();
        for (var i = 1; i <= 20; i++)
        {
            staffExtra.Add(new User
            {
                FullName = $"Le Tan {FirstNames[rng.Next(FirstNames.Length)]} {i}",
                Email = $"reception{i:D2}@demo.warranty.local",
                PasswordHash = PasswordHashing.Hash("Receptionist@123"),
                Phone = $"07{rng.Next(10000000, 99999999)}",
                Role = UserRole.Receptionist,
                IsActive = true,
                CreatedAt = now.AddDays(-rng.Next(60, 500))
            });
        }
        for (var i = 1; i <= 15; i++)
        {
            staffExtra.Add(new User
            {
                FullName = $"QL {LastNames[rng.Next(LastNames.Length)]} {FirstNames[rng.Next(FirstNames.Length)]}",
                Email = $"manager{i:D2}@demo.warranty.local",
                PasswordHash = PasswordHashing.Hash("Manager@123"),
                Phone = $"06{rng.Next(10000000, 99999999)}",
                Role = UserRole.Manager,
                IsActive = true,
                CreatedAt = now.AddDays(-rng.Next(90, 600))
            });
        }

        db.Users.AddRange(customers);
        db.Users.AddRange(technicians);
        db.Users.AddRange(staffExtra);
        await db.SaveChangesAsync();

        // --- products (~300) ---
        var productCount = await db.Products.CountAsync();
        if (productCount < 100)
        {
            var categoryIds = await db.Categories.Where(c => c.IsActive).Select(c => c.Id).ToListAsync();
            if (categoryIds.Count == 0)
            {
                var cats = new[]
                {
                    new Category { Name = "Laptop", Description = "Máy tính xách tay", IsActive = true },
                    new Category { Name = "Smartphone", Description = "Điện thoại thông minh", IsActive = true },
                    new Category { Name = "Tablet", Description = "Máy tính bảng", IsActive = true },
                    new Category { Name = "Phụ kiện", Description = "Tai nghe, bàn phím, chuột", IsActive = true },
                    new Category { Name = "Màn hình", Description = "Monitor & display", IsActive = true }
                };
                db.Categories.AddRange(cats);
                await db.SaveChangesAsync();
                categoryIds = cats.Select(c => c.Id).ToList();
            }

            var products = new List<Product>(300);
            for (var i = 1; i <= 300; i++)
            {
                var brand = Brands[rng.Next(Brands.Length)];
                var kind = ProductKinds[rng.Next(ProductKinds.Length)];
                products.Add(new Product
                {
                    Name = $"{brand} {kind} {2020 + rng.Next(0, 7)} Pro {i}",
                    Brand = brand,
                    Model = $"{kind.ToUpperInvariant()}-{rng.Next(100, 999)}",
                    SerialNumber = $"SN{brand.AsSpan(0, Math.Min(3, brand.Length)).ToString().ToUpperInvariant()}{i:D6}{rng.Next(10, 99)}",
                    WarrantyMonths = rng.Next(2, 7) * 6,
                    CategoryId = categoryIds[rng.Next(categoryIds.Count)],
                    CreatedAt = now.AddDays(-rng.Next(1, 500))
                });
            }
            db.Products.AddRange(products);
            await db.SaveChangesAsync();
        }

        // --- warranty cards for a subset of customers ---
        var cardCount = await db.WarrantyCards.CountAsync();
        if (cardCount < 200)
        {
            var productIds = await db.Products.Select(p => p.Id).Take(250).ToListAsync();
            var customerIds = await db.Users
                .Where(u => u.Role == UserRole.Customer && u.IsActive)
                .OrderBy(u => u.Id)
                .Select(u => u.Id)
                .Take(400)
                .ToListAsync();

            var cards = new List<WarrantyCard>();
            var usedProducts = new HashSet<int>();
            for (var i = 0; i < Math.Min(350, customerIds.Count) && productIds.Count > 0; i++)
            {
                var pid = productIds[rng.Next(productIds.Count)];
                if (!usedProducts.Add(pid)) continue;
                var start = now.Date.AddMonths(-rng.Next(1, 18));
                var months = rng.Next(6, 25);
                cards.Add(new WarrantyCard
                {
                    ProductId = pid,
                    CustomerId = customerIds[i % customerIds.Count],
                    StartDate = start,
                    EndDate = start.AddMonths(months),
                    Status = start.AddMonths(months) < now.Date ? WarrantyStatus.Expired : WarrantyStatus.Active
                });
            }
            if (cards.Count > 0)
            {
                db.WarrantyCards.AddRange(cards);
                await db.SaveChangesAsync();
            }
        }
    }
}
