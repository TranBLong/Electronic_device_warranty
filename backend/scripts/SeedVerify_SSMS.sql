/*
  E-Warranty — Script kiểm tra dữ liệu sau khi chạy Backend (dotnet run)
  Chạy trong SSMS sau khi API đã migrate + seed thành công.
*/
USE [Warranty];
GO

PRINT N'=== Users ===';
SELECT Id, FullName, Email, Role, IsActive, CreatedAt
FROM dbo.Users
ORDER BY Id;

PRINT N'=== Products ===';
SELECT Id, Name, Brand, Model, SerialNumber, WarrantyMonths, CategoryId
FROM dbo.Products
ORDER BY Id;

PRINT N'=== WarrantyCards ===';
SELECT wc.Id, wc.ProductId, p.Name AS ProductName, wc.CustomerId, u.FullName AS CustomerName,
       wc.StartDate, wc.EndDate, wc.Status
FROM dbo.WarrantyCards wc
JOIN dbo.Products p ON p.Id = wc.ProductId
JOIN dbo.Users u ON u.Id = wc.CustomerId
ORDER BY wc.Id;

PRINT N'=== RepairRequests ===';
SELECT rr.Id, rr.WarrantyCardId, rr.Description, rr.Status,
       rr.ReceptionistId, rr.TechnicianId, rr.CreatedAt
FROM dbo.RepairRequests rr
ORDER BY rr.Id;

PRINT N'=== ServiceCenters ===';
SELECT Id, Name, Address, Phone, IsActive FROM dbo.ServiceCenters;

PRINT N'=== Categories ===';
SELECT Id, Name, Description, IsActive FROM dbo.Categories;

PRINT N'=== Parts ===';
SELECT Id, Code, Name, UnitPrice, StockQuantity, IsActive FROM dbo.Parts;

PRINT N'=== Counts ===';
SELECT
  (SELECT COUNT(*) FROM dbo.Users) AS Users,
  (SELECT COUNT(*) FROM dbo.Products) AS Products,
  (SELECT COUNT(*) FROM dbo.WarrantyCards) AS WarrantyCards,
  (SELECT COUNT(*) FROM dbo.RepairRequests) AS RepairRequests,
  (SELECT COUNT(*) FROM dbo.ServiceCenters) AS ServiceCenters,
  (SELECT COUNT(*) FROM dbo.Categories) AS Categories,
  (SELECT COUNT(*) FROM dbo.Parts) AS Parts;
GO
