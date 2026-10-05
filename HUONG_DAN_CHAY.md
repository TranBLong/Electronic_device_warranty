# Hướng dẫn chạy E-Warranty System (Backend + Frontend + SSMS)

## Yêu cầu

| Thành phần | Phiên bản |
|------------|-----------|
| .NET SDK | 10.x |
| Node.js | 18+ (khuyến nghị 20+) |
| SQL Server | 2019+ / LocalDB / SQLEXPRESS |
| SSMS | 18+ (tùy chọn, để xem/sửa data) |

---

## 1. Cấu hình SQL Server (SSMS)

### 1.1. Tạo database trống (khuyến nghị)

Mở **SSMS** → kết nối instance (ví dụ `.\SQLEXPRESS` hoặc `(localdb)\MSSQLLocalDB`) → New Query:

```sql
IF DB_ID(N'Warranty') IS NULL
    CREATE DATABASE [Warranty];
GO
```

### 1.2. Connection string

Mở file:

`Electronic_device_warranty-main/appsettings.json`

hoặc dùng **User Secrets** / biến môi trường.

**Windows Authentication (SQLEXPRESS):**
```
Server=.\SQLEXPRESS;Database=Warranty;Trusted_Connection=True;TrustServerCertificate=True;
```

**LocalDB:**
```
Server=(localdb)\MSSQLLocalDB;Database=Warranty;Trusted_Connection=True;TrustServerCertificate=True;
```

**SQL Authentication:**
```
Server=localhost,1433;Database=Warranty;User Id=sa;Password=YourStrongPassword;TrustServerCertificate=True;
```

JWT SigningKey đã có sẵn trong `appsettings.Development.json` (chỉ dùng cho Development).

---

## 2. Chạy Backend (tự migrate + seed data)

```powershell
cd Electronic_device_warranty-main
dotnet restore
dotnet run --urls "http://localhost:5000"
```

Lần chạy đầu tiên sẽ:

1. Áp dụng toàn bộ **EF Migrations** → tạo bảng trong DB `Warranty`
2. Chạy **DatabaseSeeder** → insert user, product, warranty card, repair request, category, part, service center…

Swagger: **http://localhost:5000/swagger**

### Tài khoản demo (sau khi seed)

| Email | Password | Role |
|-------|----------|------|
| admin@warranty.local | Admin@123 | Admin |
| manager@warranty.local | Manager@123 | Manager |
| reception@warranty.local | Receptionist@123 | Receptionist |
| tech01@warranty.local | Technician@123 | Technician |
| customer01@warranty.local | Customer@123 | Customer |

> Password được hash bằng BCrypt trong code. **Không** cần (và không nên) insert plaintext password qua SSMS.

---

## 3. Xem / kiểm tra data trong SSMS

Sau khi `dotnet run` thành công:

1. Mở SSMS → Connect → chọn đúng instance
2. Expand Databases → **Warranty** → Tables
3. Các bảng chính: `Users`, `Products`, `WarrantyCards`, `RepairRequests`, `RepairRequestStatusHistories`, `Categories`, `Parts`, `ServiceCenters`, `RefreshTokens`, …

Ví dụ query:

```sql
USE [Warranty];
GO

SELECT Id, FullName, Email, Role, IsActive, CreatedAt FROM dbo.Users;
SELECT Id, Name, Brand, Model, SerialNumber, WarrantyMonths FROM dbo.Products;
SELECT Id, ProductId, CustomerId, StartDate, EndDate, Status FROM dbo.WarrantyCards;
SELECT Id, WarrantyCardId, Description, Status, TechnicianId, CreatedAt FROM dbo.RepairRequests;
SELECT Id, Name, Address, Phone FROM dbo.ServiceCenters;
SELECT Id, Code, Name, UnitPrice, StockQuantity FROM dbo.Parts;
```

### Script SQL kèm theo (tham khảo schema / dump)

Trong thư mục backend:

| File | Mô tả |
|------|--------|
| `Data/SQL/SQLData_utf8.sql` | Dump schema/data (UTF-8, mở được trong SSMS) |
| `Data/SQL/SQLData2_utf8.sql` | Dump mở rộng |
| `scripts/bootstrap-admin.sql` | Tạo admin thủ công (cần BCrypt hash) |
| `scripts/SeedVerify_SSMS.sql` | Query kiểm tra seed |

**Khuyến nghị:** để EF Migration + Seeder tạo data (đúng hash mật khẩu). Chỉ dùng file dump khi cần khôi phục schema tham khảo.

---

## 4. Chạy Frontend

```powershell
cd frontend
npm install
npm run dev
```

Mở trình duyệt: **http://localhost:5173**

API mặc định: `http://localhost:5000`  
(đổi bằng biến môi trường `VITE_API_URL` nếu cần)

Đăng nhập bằng một trong các tài khoản demo ở trên.

---

## 5. Thứ tự khởi động khuyến nghị

1. SQL Server đang chạy  
2. `dotnet run` (Backend – port 5000)  
3. `npm run dev` (Frontend – port 5173)  
4. (Tuỳ chọn) Mở SSMS xem bảng `Warranty`

---

## 6. Lỗi thường gặp

| Lỗi | Cách xử lý |
|-----|------------|
| Cannot open database / login failed | Kiểm tra connection string, SQL Server đã start, DB `Warranty` tồn tại hoặc quyền CREATE DATABASE |
| Jwt:SigningKey must be configured | Chạy với `ASPNETCORE_ENVIRONMENT=Development` hoặc set User Secrets |
| CORS / Network Error trên frontend | Backend phải chạy port 5000; CORS đã cấu hình sẵn |
| Port 5000 đã dùng | `dotnet run --urls "http://localhost:5190"` và sửa `VITE_API_URL` + CORS |
| Seed không chạy lại | Seeder chỉ insert khi bảng Users **trống**. Xóa DB hoặc drop tables rồi chạy lại |

### Reset database (SSMS)

```sql
USE master;
GO
ALTER DATABASE [Warranty] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [Warranty];
GO
CREATE DATABASE [Warranty];
GO
```

Sau đó chạy lại `dotnet run` để migrate + seed mới.

---

## 7. Cấu trúc zip

```
E-Warranty-Complete/
├── README.md
├── HUONG_DAN_CHAY.md
├── Electronic_device_warranty-main/   # Backend .NET 10
└── frontend/                         # React + Vite + Tailwind
```
