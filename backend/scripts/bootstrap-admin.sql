SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Email nvarchar(256) = LOWER(LTRIM(RTRIM(N'<admin-email>')));
DECLARE @PasswordHash nvarchar(100) = N'<bcrypt-hash-generated-locally>';

IF @Email = N'<admin-email>' OR @PasswordHash = N'<bcrypt-hash-generated-locally>'
    THROW 50001, 'Replace the email and BCrypt hash placeholders before running this script.', 1;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
    THROW 50002, 'An account already exists for this email.', 1;

INSERT INTO dbo.Users (FullName, Email, PasswordHash, Phone, Role, IsActive, CreatedAt)
VALUES (N'Warranty Administrator', @Email, @PasswordHash, NULL, N'Admin', 1, SYSUTCDATETIME());

COMMIT TRANSACTION;
