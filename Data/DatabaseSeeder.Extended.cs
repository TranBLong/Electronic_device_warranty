using Microsoft.EntityFrameworkCore;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Data;

public static partial class DatabaseSeeder
{
    public static async Task SeedExtendedAsync(AppDbContext dbContext)
    {
        // Chỉ seed 1 lần
        if (await dbContext.ServiceCenters.AnyAsync()
            || await dbContext.Categories.AnyAsync()
            || await dbContext.Parts.AnyAsync())
        {
            return;
        }

        var requiredEmails = new[]
        {
            "manager@warranty.local", "reception@warranty.local",
            "tech01@warranty.local", "customer01@warranty.local"
        };
        var requiredSerials = new[]
        {
            "DLSXPS13-001", "IP15P-2024-001", "TABS9-2024-001", "LG34-2024-001", "CANON-G2020-001"
        };

        var users = await dbContext.Users.ToDictionaryAsync(user => user.Email);
        var products = await dbContext.Products.ToDictionaryAsync(product => product.SerialNumber);
        var cardRows = await dbContext.WarrantyCards.Include(card => card.Product)
            .OrderBy(card => card.Id).ToListAsync();
        var requestRows = await dbContext.RepairRequests
            .Include(request => request.WarrantyCard).ThenInclude(card => card.Product)
            .OrderBy(request => request.Id).ToListAsync();

        var cards = cardRows.GroupBy(card => card.Product.SerialNumber)
            .ToDictionary(group => group.Key, group => group.First());
        var requests = requestRows.GroupBy(request => request.WarrantyCard.Product.SerialNumber)
            .ToDictionary(group => group.Key, group => group.First());

        // Thiếu dữ liệu gốc của seed cũ (đã bị sửa/xóa) thì bỏ qua, tránh seed sai
        if (requiredEmails.Any(email => !users.ContainsKey(email))
            || requiredSerials.Any(serial => !products.ContainsKey(serial)
                                             || !cards.ContainsKey(serial)
                                             || !requests.ContainsKey(serial)))
        {
            return;
        }

        var manager = users["manager@warranty.local"];
        var receptionist = users["reception@warranty.local"];
        var technician = users["tech01@warranty.local"];
        var customer = users["customer01@warranty.local"];

        var laptop = products["DLSXPS13-001"];
        var phone = products["IP15P-2024-001"];
        var tablet = products["TABS9-2024-001"];
        var monitor = products["LG34-2024-001"];
        var printer = products["CANON-G2020-001"];

        var rrBattery = requests["DLSXPS13-001"];
        var rrTouch = requests["IP15P-2024-001"];
        var rrPort = requests["TABS9-2024-001"];
        var rrFlicker = requests["LG34-2024-001"];
        var rrJam = requests["CANON-G2020-001"];

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        // ---------- ServiceCenters ----------
        var centers = new[]
        {
            new ServiceCenter
            {
                Name = "E-Warranty Service Center Hue",
                Address = "12 Le Loi, Phu Hoi, Hue",
                Phone = "02343822001",
                IsActive = true,
                CreatedAt = Utc(2024, 01, 10, 08, 00)
            },
            new ServiceCenter
            {
                Name = "E-Warranty Service Center Da Nang",
                Address = "254 Nguyen Van Linh, Thanh Khe, Da Nang",
                Phone = "02363650111",
                IsActive = true,
                CreatedAt = Utc(2024, 01, 10, 08, 30)
            },
            new ServiceCenter
            {
                Name = "E-Warranty Service Center Ha Noi",
                Address = "18 Tran Duy Hung, Cau Giay, Ha Noi",
                Phone = "02435550123",
                IsActive = true,
                CreatedAt = Utc(2024, 02, 01, 09, 00)
            },
            new ServiceCenter
            {
                Name = "E-Warranty Service Center Ho Chi Minh",
                Address = "72 Nguyen Hue, District 1, Ho Chi Minh City",
                Phone = "02838220456",
                IsActive = true,
                CreatedAt = Utc(2024, 02, 15, 09, 30)
            },
            new ServiceCenter
            {
                Name = "E-Warranty Service Center Can Tho",
                Address = "35 Mau Than, Ninh Kieu, Can Tho",
                Phone = "02923810789",
                IsActive = false, // đang tạm đóng cửa
                CreatedAt = Utc(2024, 03, 05, 10, 00)
            }
        };

        // ---------- Categories ----------
        var categories = new[]
        {
            new Category { Name = "Laptop", Description = "Laptops and ultrabooks.", IsActive = true, CreatedAt = Utc(2024, 01, 12, 09, 00) },
            new Category { Name = "Smartphone", Description = "Mobile phones.", IsActive = true, CreatedAt = Utc(2024, 01, 12, 09, 05) },
            new Category { Name = "Tablet", Description = "Tablets and e-readers.", IsActive = true, CreatedAt = Utc(2024, 01, 12, 09, 10) },
            new Category { Name = "Monitor", Description = "Computer monitors and displays.", IsActive = true, CreatedAt = Utc(2024, 01, 12, 09, 15) },
            new Category { Name = "Printer", Description = "Inkjet and laser printers.", IsActive = true, CreatedAt = Utc(2024, 01, 12, 09, 20) }
        };

        // ---------- Parts ----------
        var parts = new[]
        {
            new Part { Code = "BAT-DELL-XPS13", Name = "Dell XPS 13 Replacement Battery", Description = "Original 52Wh lithium-polymer battery.", UnitPrice = 1_850_000m, StockQuantity = 12, CreatedAt = Utc(2024, 02, 20, 10, 00) },
            new Part { Code = "DIG-IP15P-TOUCH", Name = "iPhone 15 Pro Touch Screen Digitizer", Description = "OLED display assembly with digitizer.", UnitPrice = 6_500_000m, StockQuantity = 5, CreatedAt = Utc(2024, 02, 20, 10, 10) },
            new Part { Code = "USBC-TABS9-PORT", Name = "Galaxy Tab S9 USB-C Charging Port Board", Description = "Charging port sub-board with flex cable.", UnitPrice = 450_000m, StockQuantity = 25, CreatedAt = Utc(2024, 02, 20, 10, 20) },
            new Part { Code = "LED-LG34-BL", Name = "LG 34-inch Monitor LED Backlight Strip", Description = "Replacement LED backlight strip set.", UnitPrice = 980_000m, StockQuantity = 8, CreatedAt = Utc(2024, 02, 20, 10, 30) },
            new Part { Code = "KIT-SCREW-ADH", Name = "Precision Screw and Adhesive Kit", Description = "Universal screws and display adhesive seal.", UnitPrice = 120_000m, StockQuantity = 60, CreatedAt = Utc(2024, 02, 20, 10, 40) }
        };

        dbContext.ServiceCenters.AddRange(centers);
        dbContext.Categories.AddRange(categories);
        dbContext.Parts.AddRange(parts);
        await dbContext.SaveChangesAsync();

        // ---------- Gắn dữ liệu cũ với dữ liệu mới ----------
        manager.ServiceCenterId = centers[0].Id;
        receptionist.ServiceCenterId = centers[0].Id;
        technician.ServiceCenterId = centers[0].Id;

        laptop.CategoryId = categories[0].Id;
        phone.CategoryId = categories[1].Id;
        tablet.CategoryId = categories[2].Id;
        monitor.CategoryId = categories[3].Id;
        printer.CategoryId = categories[4].Id;

        foreach (var request in requests.Values)
        {
            request.ServiceCenterId = centers[0].Id;
        }

        rrTouch.IsChargeable = true;   // hư hỏng do rơi
        rrTouch.LaborCost = 300_000m;
        rrPort.IsChargeable = true;    // hết hạn bảo hành
        rrPort.LaborCost = 150_000m;
        rrFlicker.IsChargeable = true; // hỏng do sốc điện
        rrFlicker.LaborCost = 200_000m;

        // ---------- 3 phiếu sửa đã trả máy (để có đủ 5 Feedback) ----------
        var extraRequests = new[]
        {
            new RepairRequest
            {
                WarrantyCardId = cards["DLSXPS13-001"].Id,
                ServiceCenterId = centers[0].Id,
                ReceptionistId = receptionist.Id,
                TechnicianId = technician.Id,
                Description = "Several keyboard keys stop responding after a firmware update.",
                Status = RepairRequestStatus.Returned,
                CreatedAt = Utc(2024, 08, 10, 09, 00),
                UpdatedAt = Utc(2024, 08, 20, 16, 00)
            },
            new RepairRequest
            {
                WarrantyCardId = cards["IP15P-2024-001"].Id,
                ServiceCenterId = centers[0].Id,
                ReceptionistId = receptionist.Id,
                TechnicianId = technician.Id,
                Description = "Rear camera autofocus keeps hunting and produces blurry photos.",
                Status = RepairRequestStatus.Returned,
                CreatedAt = Utc(2024, 10, 05, 10, 30),
                UpdatedAt = Utc(2024, 10, 14, 15, 30)
            },
            new RepairRequest
            {
                WarrantyCardId = cards["LG34-2024-001"].Id,
                ServiceCenterId = centers[0].Id,
                ReceptionistId = receptionist.Id,
                TechnicianId = technician.Id,
                Description = "Monitor has a cluster of stuck pixels near the upper-left corner.",
                Status = RepairRequestStatus.Returned,
                CreatedAt = Utc(2025, 01, 10, 11, 00),
                UpdatedAt = Utc(2025, 01, 18, 17, 00)
            }
        };

        dbContext.RepairRequests.AddRange(extraRequests);
        await dbContext.SaveChangesAsync();

        var extraHistories = new[]
        {
            new RepairRequestStatusHistory
            {
                RepairRequestId = extraRequests[0].Id,
                OldStatus = RepairRequestStatus.Completed,
                NewStatus = RepairRequestStatus.Returned,
                ChangedBy = technician.Id,
                ChangedAt = Utc(2024, 08, 20, 16, 00)
            },
            new RepairRequestStatusHistory
            {
                RepairRequestId = extraRequests[1].Id,
                OldStatus = RepairRequestStatus.Completed,
                NewStatus = RepairRequestStatus.Returned,
                ChangedBy = technician.Id,
                ChangedAt = Utc(2024, 10, 14, 15, 30)
            },
            new RepairRequestStatusHistory
            {
                RepairRequestId = extraRequests[2].Id,
                OldStatus = RepairRequestStatus.Completed,
                NewStatus = RepairRequestStatus.Returned,
                ChangedBy = technician.Id,
                ChangedAt = Utc(2025, 01, 18, 17, 00)
            }
        };

        dbContext.RepairRequestStatusHistories.AddRange(extraHistories);

        // ---------- RepairRequestParts (giá chốt tại thời điểm sửa) ----------
        var repairParts = new[]
        {
            new RepairRequestPart { RepairRequestId = rrBattery.Id, PartId = parts[0].Id, Quantity = 1, UnitPrice = parts[0].UnitPrice, AddedAt = Utc(2025, 01, 21, 09, 00) },
            new RepairRequestPart { RepairRequestId = rrTouch.Id, PartId = parts[1].Id, Quantity = 1, UnitPrice = parts[1].UnitPrice, AddedAt = Utc(2025, 02, 05, 09, 20) },
            new RepairRequestPart { RepairRequestId = rrTouch.Id, PartId = parts[4].Id, Quantity = 1, UnitPrice = parts[4].UnitPrice, AddedAt = Utc(2025, 02, 05, 09, 25) },
            new RepairRequestPart { RepairRequestId = rrPort.Id, PartId = parts[2].Id, Quantity = 1, UnitPrice = parts[2].UnitPrice, AddedAt = Utc(2024, 07, 08, 10, 00) },
            new RepairRequestPart { RepairRequestId = rrFlicker.Id, PartId = parts[3].Id, Quantity = 1, UnitPrice = parts[3].UnitPrice, AddedAt = Utc(2025, 04, 15, 13, 00) }
        };

        dbContext.RepairRequestParts.AddRange(repairParts);

        // ---------- Invoices ----------
        // TotalAmount = số tiền khách phải trả = (IsChargeable ? PartsTotal + LaborTotal : 0)
        var invoices = new[]
        {
            new Invoice
            {
                RepairRequestId = rrBattery.Id,
                InvoiceNumber = "INV-2025-0001",
                PartsTotal = 1_850_000m,
                LaborTotal = 0m,
                TotalAmount = 0m, // trong bảo hành, miễn phí
                Status = InvoiceStatus.Paid,
                IssuedAt = Utc(2025, 01, 20, 08, 50),
                PaidAt = Utc(2025, 01, 20, 08, 50)
            },
            new Invoice
            {
                RepairRequestId = rrTouch.Id,
                InvoiceNumber = "INV-2025-0002",
                PartsTotal = 6_620_000m,
                LaborTotal = 300_000m,
                TotalAmount = 6_920_000m,
                Status = InvoiceStatus.Unpaid, // mới đặt cọc 4.000.000
                IssuedAt = Utc(2025, 02, 02, 11, 00),
                PaidAt = null
            },
            new Invoice
            {
                RepairRequestId = rrPort.Id,
                InvoiceNumber = "INV-2024-0001",
                PartsTotal = 450_000m,
                LaborTotal = 150_000m,
                TotalAmount = 600_000m,
                Status = InvoiceStatus.Paid,
                IssuedAt = Utc(2024, 07, 05, 15, 00),
                PaidAt = Utc(2024, 07, 11, 16, 30)
            },
            new Invoice
            {
                RepairRequestId = rrFlicker.Id,
                InvoiceNumber = "INV-2025-0003",
                PartsTotal = 980_000m,
                LaborTotal = 200_000m,
                TotalAmount = 1_180_000m,
                Status = InvoiceStatus.Paid,
                IssuedAt = Utc(2025, 04, 18, 16, 45),
                PaidAt = Utc(2025, 04, 18, 17, 00)
            },
            new Invoice
            {
                RepairRequestId = rrJam.Id,
                InvoiceNumber = "INV-2023-0001",
                PartsTotal = 0m,
                LaborTotal = 0m,
                TotalAmount = 0m,
                Status = InvoiceStatus.Cancelled, // phiếu sửa đã hủy
                IssuedAt = Utc(2023, 04, 28, 13, 50),
                PaidAt = null
            }
        };

        dbContext.Invoices.AddRange(invoices);
        await dbContext.SaveChangesAsync();

        // ---------- Payments ----------
        var payments = new[]
        {
            new Payment { InvoiceId = invoices[1].Id, ReceivedById = receptionist.Id, Amount = 2_000_000m, Method = PaymentMethod.BankTransfer, Note = "Deposit at drop-off.", PaidAt = Utc(2025, 02, 02, 11, 30) },
            new Payment { InvoiceId = invoices[1].Id, ReceivedById = receptionist.Id, Amount = 2_000_000m, Method = PaymentMethod.Card, Note = "Second deposit after parts ordered.", PaidAt = Utc(2025, 02, 05, 09, 30) },
            new Payment { InvoiceId = invoices[2].Id, ReceivedById = receptionist.Id, Amount = 300_000m, Method = PaymentMethod.Cash, Note = "Deposit at drop-off.", PaidAt = Utc(2024, 07, 05, 15, 10) },
            new Payment { InvoiceId = invoices[2].Id, ReceivedById = receptionist.Id, Amount = 300_000m, Method = PaymentMethod.BankTransfer, Note = "Remaining balance on completion.", PaidAt = Utc(2024, 07, 11, 16, 30) },
            new Payment { InvoiceId = invoices[3].Id, ReceivedById = receptionist.Id, Amount = 1_180_000m, Method = PaymentMethod.BankTransfer, Note = "Full payment on pickup.", PaidAt = Utc(2025, 04, 18, 17, 00) }
        };

        dbContext.Payments.AddRange(payments);

        // ---------- Notifications ----------
        var notifications = new[]
        {
            new Notification
            {
                UserId = customer.Id,
                RepairRequestId = rrBattery.Id,
                Type = NotificationType.RepairStatusChanged,
                Title = "Repair request received",
                Message = $"Your repair request #{rrBattery.Id} for Dell XPS 13 has been received at E-Warranty Service Center Hue.",
                IsRead = true,
                CreatedAt = Utc(2025, 01, 20, 08, 45)
            },
            new Notification
            {
                UserId = technician.Id,
                RepairRequestId = rrTouch.Id,
                Type = NotificationType.TechnicianAssigned,
                Title = "New repair assigned",
                Message = $"You have been assigned repair request #{rrTouch.Id}: iPhone 15 Pro touchscreen is unresponsive.",
                IsRead = true,
                CreatedAt = Utc(2025, 02, 02, 10, 10)
            },
            new Notification
            {
                UserId = customer.Id,
                RepairRequestId = rrTouch.Id,
                Type = NotificationType.RepairStatusChanged,
                Title = "Repair in progress",
                Message = $"Repair request #{rrTouch.Id} for iPhone 15 Pro is now in progress.",
                IsRead = false,
                CreatedAt = Utc(2025, 02, 05, 09, 20)
            },
            new Notification
            {
                UserId = customer.Id,
                RepairRequestId = rrFlicker.Id,
                Type = NotificationType.RepairStatusChanged,
                Title = "Device returned",
                Message = $"Repair request #{rrFlicker.Id} is complete and your LG UltraWide monitor has been returned.",
                IsRead = true,
                CreatedAt = Utc(2025, 04, 18, 17, 35)
            },
            new Notification
            {
                UserId = customer.Id,
                RepairRequestId = null,
                Type = NotificationType.WarrantyExpiring,
                Title = "Warranty expiring soon",
                Message = "The warranty for LG UltraWide Monitor (LG34-2024-001) expires on 20/11/2025.",
                IsRead = false,
                CreatedAt = Utc(2025, 10, 21, 08, 00)
            }
        };

        dbContext.Notifications.AddRange(notifications);

        // ---------- Attachments ----------
        var attachments = new[]
        {
            new RepairRequestAttachment { RepairRequestId = rrBattery.Id, UploadedById = receptionist.Id, FileName = "battery-health-report.png", FilePath = $"uploads/repair-requests/{rrBattery.Id}/battery-health-report.png", ContentType = "image/png", FileSize = 245_760, UploadedAt = Utc(2025, 01, 20, 08, 42) },
            new RepairRequestAttachment { RepairRequestId = rrTouch.Id, UploadedById = receptionist.Id, FileName = "cracked-screen-front.jpg", FilePath = $"uploads/repair-requests/{rrTouch.Id}/cracked-screen-front.jpg", ContentType = "image/jpeg", FileSize = 1_482_310, UploadedAt = Utc(2025, 02, 02, 10, 05) },
            new RepairRequestAttachment { RepairRequestId = rrTouch.Id, UploadedById = technician.Id, FileName = "diagnostic-report.pdf", FilePath = $"uploads/repair-requests/{rrTouch.Id}/diagnostic-report.pdf", ContentType = "application/pdf", FileSize = 312_448, UploadedAt = Utc(2025, 02, 05, 09, 10) },
            new RepairRequestAttachment { RepairRequestId = rrPort.Id, UploadedById = customer.Id, FileName = "charging-port-closeup.jpg", FilePath = $"uploads/repair-requests/{rrPort.Id}/charging-port-closeup.jpg", ContentType = "image/jpeg", FileSize = 986_112, UploadedAt = Utc(2024, 07, 05, 14, 15) },
            new RepairRequestAttachment { RepairRequestId = rrFlicker.Id, UploadedById = customer.Id, FileName = "monitor-flicker.mp4", FilePath = $"uploads/repair-requests/{rrFlicker.Id}/monitor-flicker.mp4", ContentType = "video/mp4", FileSize = 12_582_912, UploadedAt = Utc(2025, 04, 12, 11, 50) }
        };

        dbContext.RepairRequestAttachments.AddRange(attachments);

        // ---------- Feedbacks (chỉ cho phiếu đã Completed/Returned, mỗi phiếu 1 đánh giá) ----------
        var feedbacks = new[]
        {
            new Feedback { RepairRequestId = rrPort.Id, CustomerId = customer.Id, Rating = 4, Comment = "Fixed quickly, but I wish the warranty had covered it.", CreatedAt = Utc(2024, 07, 13, 09, 00) },
            new Feedback { RepairRequestId = rrFlicker.Id, CustomerId = customer.Id, Rating = 4, Comment = "Technician explained the power surge cause clearly.", CreatedAt = Utc(2025, 04, 20, 10, 00) },
            new Feedback { RepairRequestId = extraRequests[0].Id, CustomerId = customer.Id, Rating = 5, Comment = "Keyboard works perfectly again. Very professional staff.", CreatedAt = Utc(2024, 08, 22, 08, 30) },
            new Feedback { RepairRequestId = extraRequests[1].Id, CustomerId = customer.Id, Rating = 3, Comment = "Camera is fixed, but the repair took longer than expected.", CreatedAt = Utc(2024, 10, 16, 09, 15) },
            new Feedback { RepairRequestId = extraRequests[2].Id, CustomerId = customer.Id, Rating = 5, Comment = "Stuck pixels gone and the monitor was returned within a week.", CreatedAt = Utc(2025, 01, 20, 10, 45) }
        };

        dbContext.Feedbacks.AddRange(feedbacks);

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);
}
