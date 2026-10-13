SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @ManagerId int =
(
    SELECT TOP (1) Id
    FROM dbo.Users
    WHERE Role = N'Manager' AND IsActive = 1
    ORDER BY Id
);

IF @ManagerId IS NULL
    THROW 50003, 'Create an active Manager account before seeding service centers.', 1;

DECLARE @Centers TABLE
(
    Name nvarchar(200) NOT NULL,
    Address nvarchar(500) NOT NULL,
    Phone nvarchar(30) NULL
);

INSERT INTO @Centers (Name, Address, Phone)
VALUES
    (N'E-Warranty Service Center Hue', N'12 Le Loi, Phu Hoi, Hue', N'02343822001'),
    (N'E-Warranty Service Center Da Nang', N'254 Nguyen Van Linh, Thanh Khe, Da Nang', N'02363650111'),
    (N'E-Warranty Service Center Ha Noi', N'18 Tran Duy Hung, Cau Giay, Ha Noi', N'02435550123');

INSERT INTO dbo.ServiceCenters (Name, Address, ManagerId, Phone, IsActive, CreatedAt)
SELECT sample.Name, sample.Address, @ManagerId, sample.Phone, 1, SYSUTCDATETIME()
FROM @Centers AS sample
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.ServiceCenters AS existing
    WHERE existing.Name = sample.Name
);

COMMIT TRANSACTION;
