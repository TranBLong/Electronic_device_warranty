using Microsoft.EntityFrameworkCore;
using Warranty.Enums;
using Warranty.Models;
using Warranty.Services;

namespace Warranty.Data;

public static partial class DatabaseSeeder
{
    /// <summary>
    /// Bổ sung dữ liệu demo tiếng Việt và bỏ qua các bản ghi đã tồn tại.
    /// </summary>
    public static async Task SeedVietnameseDemoAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var rng = new Random(2811);

        if (await db.Categories.CountAsync() < 20)
        {
            var definitions = new[]
            {
                new { Name = "Tai nghe", Description = "Tai nghe có dây / không dây, true wireless" },
                new { Name = "Chuot may tinh", Description = "Chuot co day, khong day, gaming" },
                new { Name = "Ban phim", Description = "Ban phim co, membrane, khong day" },
                new { Name = "O cung SSD", Description = "SSD SATA, NVMe M.2" },
                new { Name = "Router Wi-Fi", Description = "Bo phat song Wi-Fi gia dinh / van phong" },
                new { Name = "Loa bluetooth", Description = "Loa di dong, loa thanh" },
                new { Name = "May anh", Description = "May anh ky thuat so, mirrorless" },
                new { Name = "Dong ho thong minh", Description = "Smartwatch, vong deo tay suc khoe" },
                new { Name = "May tinh de ban", Description = "PC de ban, all-in-one" },
                new { Name = "Phu kien sac", Description = "Sac nhanh, pin du phong, cap" },
                new { Name = "Webcam", Description = "Camera hoi nghi, livestream" },
                new { Name = "May chieu", Description = "May chieu van phong, gia dinh" },
                new { Name = "Thiet bi luu tru", Description = "USB, the nho, o cung di dong" },
                new { Name = "Thiet bi mang", Description = "Switch, access point, modem" },
                new { Name = "May in da nang", Description = "In-scan-copy, laser / phun muc" }
            };

            var existingNames = await db.Categories
                .Select(category => category.Name)
                .ToHashSetAsync();
            var categories = definitions
                .Where(definition => !existingNames.Contains(definition.Name))
                .Select(definition => new Category
                {
                    Name = definition.Name,
                    Description = definition.Description,
                    IsActive = true,
                    CreatedAt = now.AddDays(-rng.Next(5, 121))
                })
                .ToList();

            if (categories.Count > 0)
            {
                db.Categories.AddRange(categories);
                await db.SaveChangesAsync();
            }
        }

        if (await db.ServiceCenters.CountAsync() < 20)
        {
            var definitions = new[]
            {
                ("Trung tam BH Ha Noi - Cau Giay", "18 Tran Duy Hung, Cau Giay, Ha Noi", "02435550123", true),
                ("Trung tam BH Ha Noi - Long Bien", "45 Nguyen Van Cu, Long Bien, Ha Noi", "02438765432", true),
                ("Trung tam BH TP.HCM - Quan 1", "72 Nguyen Hue, Quan 1, TP.HCM", "02838220456", true),
                ("Trung tam BH TP.HCM - Tan Binh", "128 Hoang Van Thu, Tan Binh, TP.HCM", "02838451234", true),
                ("Trung tam BH Da Nang", "254 Nguyen Van Linh, Thanh Khe, Da Nang", "02363650111", true),
                ("Trung tam BH Hue", "12 Le Loi, Phu Hoi, Hue", "02343822001", true),
                ("Trung tam BH Can Tho", "35 Mau Than, Ninh Kieu, Can Tho", "02923810789", true),
                ("Trung tam BH Hai Phong", "88 Lach Tray, Ngo Quyen, Hai Phong", "02253881234", true),
                ("Trung tam BH Nha Trang", "15 Tran Phu, Loc Tho, Nha Trang", "02583824567", true),
                ("Trung tam BH Bien Hoa", "220 Pham Van Thuan, Bien Hoa, Dong Nai", "02513827890", true),
                ("Trung tam BH Vung Tau", "56 Hoang Hoa Tham, Vung Tau", "02543821234", false),
                ("Trung tam BH Buon Ma Thuot", "12 Nguyen Tat Thanh, Buon Ma Thuot", "02623825678", true),
                ("Trung tam BH Quy Nhon", "78 Le Hong Phong, Quy Nhon", "02563829012", true),
                ("Trung tam BH Vinh", "33 Nguyen Sy Sach, Vinh, Nghe An", "02383823456", true),
                ("Trung tam BH Thai Nguyen", "19 Hoang Van Thu, Thai Nguyen", "02083826789", true)
            };

            var existingNames = await db.ServiceCenters
                .Select(center => center.Name)
                .ToHashSetAsync();
            var centers = definitions
                .Where(definition => !existingNames.Contains(definition.Item1))
                .Select(definition => new ServiceCenter
                {
                    Name = definition.Item1,
                    Address = definition.Item2,
                    Phone = definition.Item3,
                    IsActive = definition.Item4,
                    CreatedAt = now.AddDays(-rng.Next(30, 201))
                })
                .ToList();

            if (centers.Count > 0)
            {
                db.ServiceCenters.AddRange(centers);
                await db.SaveChangesAsync();
            }
        }

        if (await db.Parts.CountAsync() < 20)
        {
            var definitions = new[]
            {
                ("PIN-IP15", "Pin iPhone 15 chinh hang", "Pin lithium-ion dung luong goc", 1_200_000m, 30),
                ("MAN-SS-A54", "Man hinh Samsung A54", "Man Super AMOLED zin", 2_800_000m, 12),
                ("BANPHIM-DELL", "Ban phim laptop Dell Inspiron", "Ban phim tieng Viet co den", 650_000m, 20),
                ("SSD-512-NVME", "O SSD NVMe 512GB", "SSD M.2 NVMe Gen3 512GB", 1_150_000m, 40),
                ("QUAT-LAPTOP", "Quat tan nhiet laptop", "Quat CPU laptop pho thong", 280_000m, 50),
                ("CAM-IP14", "Camera sau iPhone 14", "Module camera chinh", 3_500_000m, 8),
                ("SAC-65W", "Sac nhanh 65W USB-C", "Adapter sac laptop / dien thoai", 450_000m, 60),
                ("LOA-LAPTOP", "Loa laptop doi", "Cap loa laptop 8 Ohm", 180_000m, 35),
                ("MAIN-ASUS", "Mainboard laptop Asus VivoBook", "Bo mach chu thay the", 4_200_000m, 6),
                ("CAMUNG-TAB", "Kinh cam ung may tinh bang", "Digitizer tablet 10.5 inch", 1_600_000m, 15),
                ("RAM-8GB-DDR4", "RAM DDR4 8GB laptop", "Thanh RAM SODIMM 3200MHz", 520_000m, 45),
                ("JACK-SAC", "Jack sac USB-C laptop", "Cong sac USB-C thay the", 220_000m, 55),
                ("VO-IP13", "Vo lung iPhone 13", "Nap lung kinh zin", 890_000m, 18),
                ("CABLE-FLEX", "Cap flex man hinh", "Cap mem ket noi man hinh laptop", 350_000m, 25),
                ("PIN-DELL-LAT", "Pin Dell Latitude 5420", "Pin 4-cell 63Wh", 1_750_000m, 10)
            };

            var existingCodes = await db.Parts
                .Select(part => part.Code)
                .ToHashSetAsync();
            var parts = definitions
                .Where(definition => !existingCodes.Contains(definition.Item1))
                .Select(definition => new Part
                {
                    Code = definition.Item1,
                    Name = definition.Item2,
                    Description = definition.Item3,
                    UnitPrice = definition.Item4,
                    StockQuantity = definition.Item5,
                    IsActive = true,
                    CreatedAt = now.AddDays(-rng.Next(20, 101))
                })
                .ToList();

            if (parts.Count > 0)
            {
                db.Parts.AddRange(parts);
                await db.SaveChangesAsync();
            }
        }

        var customerEmails = Enumerable.Range(1, 20)
            .Select(index => $"khach.vn{index:D2}@warranty.local")
            .ToList();
        var existingCustomerEmails = await db.Users
            .Where(user => customerEmails.Contains(user.Email))
            .Select(user => user.Email)
            .ToHashSetAsync();

        if (existingCustomerEmails.Count < 20)
        {
            var fullNames = new[]
            {
                "Nguyen Van Hung", "Tran Thi Mai", "Le Minh Tuan", "Pham Thi Lan", "Hoang Van Nam",
                "Vu Thi Huong", "Dang Quoc Bao", "Bui Thi Ngoc", "Do Van Khoa", "Ngo Thi Thu",
                "Duong Minh Duc", "Ly Thi Ha", "Truong Van Phuc", "Phan Thi Yen", "Vo Minh Hai",
                "Huynh Thi Trang", "Cao Van Long", "Lam Thi Phuong", "To Minh Quan", "Dinh Thi Oanh"
            };
            var passwordHash = PasswordHashing.Hash("Customer@123");
            var customers = new List<User>();

            for (var i = 0; i < customerEmails.Count; i++)
            {
                if (existingCustomerEmails.Contains(customerEmails[i]))
                    continue;

                customers.Add(new User
                {
                    FullName = fullNames[i],
                    Email = customerEmails[i],
                    PasswordHash = passwordHash,
                    Phone = $"09{rng.Next(10000000, 99999999)}",
                    Role = UserRole.Customer,
                    IsActive = true,
                    CreatedAt = now.AddDays(-rng.Next(10, 201))
                });
            }

            if (customers.Count > 0)
            {
                db.Users.AddRange(customers);
                await db.SaveChangesAsync();
            }
        }

        var serials = Enumerable.Range(1, 20)
            .Select(index => $"VN-SP-{index:D4}")
            .ToList();
        var existingSerials = await db.Products
            .Where(product => serials.Contains(product.SerialNumber))
            .Select(product => product.SerialNumber)
            .ToHashSetAsync();

        if (existingSerials.Count < 20)
        {
            var categoryIds = await db.Categories
                .Where(category => category.IsActive)
                .Select(category => category.Id)
                .ToListAsync();
            if (categoryIds.Count == 0)
                return;

            var definitions = new (string Name, string Brand, string Model, int Months)[]
            {
                ("Laptop Dell Inspiron 15", "Dell", "Inspiron 3520", 24),
                ("iPhone 15 128GB", "Apple", "A3090", 12),
                ("Samsung Galaxy S24", "Samsung", "SM-S921B", 12),
                ("MacBook Air M2", "Apple", "MLY33", 12),
                ("Laptop ASUS VivoBook 15", "Asus", "X1504ZA", 24),
                ("iPad Air 5", "Apple", "MM9C3", 12),
                ("May in Canon LBP2900", "Canon", "LBP2900", 12),
                ("Man hinh LG 27 inch", "LG", "27UP850", 24),
                ("Tai nghe Sony WH-1000XM5", "Sony", "WH1000XM5", 12),
                ("Xiaomi Redmi Note 13", "Xiaomi", "2312DRA50G", 12),
                ("Laptop HP Pavilion 14", "HP", "14-dv2073TU", 24),
                ("Lenovo ThinkPad E14", "Lenovo", "E14 Gen 5", 36),
                ("Loa JBL Flip 6", "JBL", "Flip6", 12),
                ("Router TP-Link AX1800", "TP-Link", "Archer AX21", 24),
                ("O SSD Samsung 980 500GB", "Samsung", "MZ-V8V500BW", 60),
                ("Chuot Logitech MX Master 3S", "Logitech", "MX Master 3S", 24),
                ("Ban phim co Keychron K2", "Keychron", "K2 V2", 12),
                ("Webcam Logitech C920", "Logitech", "C920 Pro", 24),
                ("Dong ho Apple Watch SE", "Apple", "MR213", 12),
                ("May chieu Epson EB-E01", "Epson", "EB-E01", 24)
            };

            var products = new List<Product>();
            for (var i = 0; i < definitions.Length; i++)
            {
                if (existingSerials.Contains(serials[i]))
                    continue;

                var definition = definitions[i];
                products.Add(new Product
                {
                    Name = definition.Name,
                    Brand = definition.Brand,
                    Model = definition.Model,
                    SerialNumber = serials[i],
                    WarrantyMonths = definition.Months,
                    CategoryId = categoryIds[rng.Next(categoryIds.Count)],
                    CreatedAt = now.AddDays(-rng.Next(20, 301))
                });
            }

            if (products.Count > 0)
            {
                db.Products.AddRange(products);
                await db.SaveChangesAsync();
            }
        }

        var vietProducts = await db.Products
            .Where(product => product.SerialNumber.StartsWith("VN-SP-"))
            .OrderBy(product => product.Id)
            .Take(20)
            .ToListAsync();
        var vietCustomerEmails = Enumerable.Range(1, 20)
            .Select(index => $"khach.vn{index:D2}@warranty.local")
            .ToList();
        var vietCustomers = await db.Users
            .Where(user => vietCustomerEmails.Contains(user.Email)
                && user.Role == UserRole.Customer
                && user.IsActive)
            .OrderBy(user => user.Id)
            .ToListAsync();
        var receptionist = await db.Users
            .Where(user => user.Role == UserRole.Receptionist && user.IsActive)
            .OrderBy(user => user.Id)
            .FirstOrDefaultAsync();
        var technician = await db.Users
            .Where(user => user.Role == UserRole.Technician && user.IsActive)
            .OrderBy(user => user.Id)
            .FirstOrDefaultAsync();
        var center = await db.ServiceCenters
            .Where(serviceCenter => serviceCenter.IsActive)
            .OrderBy(serviceCenter => serviceCenter.Id)
            .FirstOrDefaultAsync();

        if (vietProducts.Count == 0 || vietCustomers.Count == 0
            || receptionist == null || technician == null)
            return;

        var existingProductIds = await db.WarrantyCards
            .Where(card => vietProducts.Select(product => product.Id).Contains(card.ProductId))
            .Select(card => card.ProductId)
            .ToHashSetAsync();
        var newCards = new List<WarrantyCard>();

        for (var i = 0; i < vietProducts.Count; i++)
        {
            var product = vietProducts[i];
            if (existingProductIds.Contains(product.Id))
                continue;

            var startDate = now.Date.AddMonths(-rng.Next(1, 14));
            var endDate = startDate.AddMonths(product.WarrantyMonths > 0 ? product.WarrantyMonths : 12);
            newCards.Add(new WarrantyCard
            {
                ProductId = product.Id,
                CustomerId = vietCustomers[i % vietCustomers.Count].Id,
                StartDate = startDate,
                EndDate = endDate,
                Status = endDate < now.Date ? WarrantyStatus.Expired : WarrantyStatus.Active
            });
        }

        if (newCards.Count > 0)
        {
            db.WarrantyCards.AddRange(newCards);
            await db.SaveChangesAsync();
        }

        var cardList = await db.WarrantyCards
            .Include(card => card.Product)
            .Where(card => card.Product.SerialNumber.StartsWith("VN-SP-"))
            .OrderBy(card => card.ProductId)
            .Take(20)
            .ToListAsync();
        if (cardList.Count == 0)
            return;

        var existingCardIds = await db.RepairRequests
            .Where(request => cardList.Select(card => card.Id).Contains(request.WarrantyCardId))
            .Select(request => request.WarrantyCardId)
            .ToHashSetAsync();

        var descriptions = new[]
        {
            "Pin chai nhanh, máy tự tắt dù vẫn còn khoảng 30% pin.",
            "Màn hình xuất hiện sọc dọc màu hồng, khởi động lại vẫn không hết.",
            "Thiết bị không nhận sạc, cắm nguồn nhưng không lên điện.",
            "Loa bị rè khi tăng âm lượng trên mức 50%.",
            "Một số phím A, S, D trên bàn phím không hoạt động.",
            "Camera sau bị mờ và không lấy nét tự động.",
            "Máy nóng bất thường khi mở nhiều tab trình duyệt.",
            "Cổng USB-C bị lỏng, cáp dễ tuột khi di chuyển thiết bị.",
            "Màn hình cảm ứng không phản hồi ở góc bên phải.",
            "Wi-Fi thường xuyên mất kết nối, phải tắt rồi bật lại.",
            "Quạt tản nhiệt kêu to hơn bình thường khi sử dụng.",
            "Màn hình nhấp nháy khi chạy phần mềm thiết kế.",
            "Máy in thường xuyên kẹt giấy dù đã vệ sinh khay nạp.",
            "Tai nghe chỉ phát âm thanh ở một bên.",
            "Đồng hồ thông minh không đồng bộ dữ liệu với điện thoại.",
            "Bộ định tuyến phát sóng yếu, phòng ở xa thường bị mất mạng.",
            "Ổ SSD báo lỗi SMART, máy khởi động chậm hơn trước.",
            "Chuột không dây bị giật, lag dù đã thay pin mới.",
            "Webcam cho hình ảnh quá tối, không điều chỉnh được độ sáng.",
            "Máy chiếu hiển thị một góc màn hình bị ố vàng."
        };
        var statuses = new[]
        {
            RepairRequestStatus.Received,
            RepairRequestStatus.InProgress,
            RepairRequestStatus.Completed,
            RepairRequestStatus.Returned,
            RepairRequestStatus.Cancelled
        };
        var requests = new List<RepairRequest>();
        var requestHistorySteps = new Dictionary<RepairRequest,
            List<(RepairRequestStatus? OldStatus, RepairRequestStatus NewStatus, int ChangedBy, DateTime ChangedAt)>>();

        for (var i = 0; i < cardList.Count; i++)
        {
            var card = cardList[i];
            if (existingCardIds.Contains(card.Id))
                continue;

            var status = statuses[i % statuses.Length];
            var createdAt = now.AddDays(-rng.Next(3, 90)).AddHours(-rng.Next(0, 12));
            var updatedAt = status == RepairRequestStatus.Received
                ? createdAt
                : createdAt.AddDays(rng.Next(1, 10));
            var assignedTechnician = status is RepairRequestStatus.InProgress
                or RepairRequestStatus.Completed
                or RepairRequestStatus.Returned
                    ? technician.Id
                    : (int?)null;
            var isCompleted = status is RepairRequestStatus.Completed or RepairRequestStatus.Returned;

            var request = new RepairRequest
            {
                WarrantyCardId = card.Id,
                ServiceCenterId = center?.Id,
                ReceptionistId = receptionist.Id,
                TechnicianId = assignedTechnician,
                Description = descriptions[i % descriptions.Length],
                Status = status,
                IsChargeable = isCompleted && card.Status == WarrantyStatus.Expired,
                LaborCost = isCompleted ? rng.Next(1, 6) * 100_000m : 0m,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };
            requests.Add(request);

            var history = new List<(RepairRequestStatus? OldStatus, RepairRequestStatus NewStatus, int ChangedBy, DateTime ChangedAt)>
            {
                (null, RepairRequestStatus.Received, receptionist.Id, createdAt)
            };
            if (status == RepairRequestStatus.InProgress)
            {
                history.Add((RepairRequestStatus.Received, status, technician.Id, updatedAt));
            }
            else if (isCompleted)
            {
                var elapsedTicks = (updatedAt - createdAt).Ticks;
                var inProgressAt = createdAt.AddTicks(elapsedTicks / 3);
                var completedAt = createdAt.AddTicks(elapsedTicks * 2 / 3);
                history.Add((RepairRequestStatus.Received, RepairRequestStatus.InProgress, technician.Id, inProgressAt));
                history.Add((RepairRequestStatus.InProgress, RepairRequestStatus.Completed, technician.Id, completedAt));
                if (status == RepairRequestStatus.Returned)
                    history.Add((RepairRequestStatus.Completed, status, technician.Id, updatedAt));
            }
            else if (status == RepairRequestStatus.Cancelled)
            {
                history.Add((RepairRequestStatus.Received, status, receptionist.Id, updatedAt));
            }

            requestHistorySteps.Add(request, history);
        }

        if (requests.Count == 0)
            return;

        db.RepairRequests.AddRange(requests);
        await db.SaveChangesAsync();

        var histories = requests.SelectMany(request =>
            requestHistorySteps[request].Select(step => new RepairRequestStatusHistory
            {
                RepairRequestId = request.Id,
                OldStatus = step.OldStatus,
                NewStatus = step.NewStatus,
                ChangedBy = step.ChangedBy,
                ChangedAt = step.ChangedAt
            }));
        db.RepairRequestStatusHistories.AddRange(histories);
        await db.SaveChangesAsync();
    }
}
