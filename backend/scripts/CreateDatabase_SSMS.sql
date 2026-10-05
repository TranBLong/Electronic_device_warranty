/*
  E-Warranty — Tạo database trống trên SQL Server
  Chạy trong SSMS trước lần đầu chạy Backend.
  Schema + data sẽ do EF Migration + DatabaseSeeder tạo khi `dotnet run`.
*/
USE master;
GO

IF DB_ID(N'Warranty') IS NULL
BEGIN
    CREATE DATABASE [Warranty];
    PRINT N'Database Warranty đã được tạo.';
END
ELSE
BEGIN
    PRINT N'Database Warranty đã tồn tại.';
END
GO

/*
  Nếu cần XÓA hết và tạo lại (mất toàn bộ data):

USE master;
GO
IF DB_ID(N'Warranty') IS NOT NULL
BEGIN
    ALTER DATABASE [Warranty] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [Warranty];
END
GO
CREATE DATABASE [Warranty];
GO
*/
