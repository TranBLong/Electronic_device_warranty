# E-Warranty System

Hệ thống **Quản lý bảo hành thiết bị điện tử** — Backend ASP.NET Core (.NET 10) + Frontend React (Vite + TypeScript + Tailwind).

Repo: [TranBLong/Electronic_device_warranty](https://github.com/TranBLong/Electronic_device_warranty)

## Cấu trúc

```
├── backend/          # ASP.NET Core API + EF Core + SQL Server
├── frontend/         # React SPA
├── HUONG_DAN_CHAY.md # Hướng dẫn chạy chi tiết
└── README.md
```

## Vai trò

Admin · Manager · Receptionist · Technician · Customer

## Chạy nhanh

### 1. SQL Server (SSMS)

```sql
IF DB_ID(N'Warranty') IS NULL CREATE DATABASE [Warranty];
```

Connection string mặc định (`backend/appsettings.json`):

```
Server=localhost\SQLEXPRESS;Database=Warranty;Trusted_Connection=True;TrustServerCertificate=True;
```

### 2. Backend

```bash
cd backend
dotnet restore
dotnet run --urls "http://localhost:5000"
```

Swagger: http://localhost:5000/swagger
- Migrate + seed chạy tự động lần đầu

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

→ http://localhost:5173

## Tài khoản demo

| Email | Password | Role |
|-------|----------|------|
| admin@warranty.local | Admin@123 | Admin |
| manager@warranty.local | Manager@123 | Manager |
| reception@warranty.local | Receptionist@123 | Receptionist |
| tech01@warranty.local | Technician@123 | Technician |
| customer01@warranty.local | Customer@123 | Customer |

Các tài khoản trên được `DatabaseSeeder` tạo trong môi trường Development khi bảng Users còn trống; đây là thông tin demo, không dùng trong production.

## Bảo mật

- Mật khẩu: **BCrypt** (`PasswordHashing.Hash` / `Verify`)
- Auth: JWT Access Token (30 phút) + Refresh Token (7 ngày, rotate)
- Soft-delete user (`IsActive = false`)

## API chính

| Method | Route | Quyền |
|--------|-------|-------|
| POST | /api/auth/register | Anonymous (Customer) |
| POST | /api/auth/login | Anonymous |
| POST | /api/auth/refresh | Anonymous |
| CRUD | /api/users | Admin |
| CRUD | /api/products | Read: auth · Write: Admin |
| GET/POST | /api/warranty-cards | Theo role |
| GET/POST | /api/repair-requests | Theo role |
| PUT | /api/repair-requests/{id}/technician | Receptionist, Manager |
| PUT | /api/repair-requests/{id}/status | Technician được gán |
| GET | /api/reports/summary | Admin, Manager |
| CRUD | /api/categories, /api/parts, /api/service-centers | Theo role |

Chi tiết: xem `HUONG_DAN_CHAY.md`.
