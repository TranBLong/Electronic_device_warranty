# E-Warranty System — Hệ thống Quản lý Bảo hành Thiết bị Điện tử

Hệ thống đầy đủ **Backend (ASP.NET Core .NET 10)** + **Frontend (React + TypeScript + Vite + Tailwind)**.

## Vai trò

| Vai trò | Quyền chính |
|---------|-------------|
| **Admin** | CRUD user, product, category, part, service center; xem mọi dữ liệu; báo cáo |
| **Manager** | Báo cáo, gán technician, quản lý part/service center |
| **Receptionist** | Tạo WarrantyCard, tạo RepairRequest, gán Technician, xem khách hàng |
| **Technician** | Xem job được giao, cập nhật trạng thái sửa chữa |
| **Customer** | Xem phiếu BH của mình, tạo yêu cầu SC, theo dõi tiến độ |

## Cấu trúc

```
artifacts/
├── Electronic_device_warranty-main/   # Backend API
│   ├── Program.cs                    # CORS đã thêm
│   ├── Data/                         # DbContext + Seeder
│   ├── Warranty/
│   │   ├── Controllers/Important/    # Auth, Users, Products, WarrantyCards,
│   │   │                             # RepairRequests, Reports, Categories,
│   │   │                             # Parts, ServiceCenters
│   │   ├── Models/                   # Full domain model
│   │   ├── Dtos/
│   │   └── Services/
│   └── ...
└── frontend/                         # React SPA
    ├── src/
    │   ├── api/                      # Axios client + refresh token
    │   ├── pages/                    # Dashboard, Products, Warranty, Repair, Users...
    │   ├── layouts/
    │   ├── store/                    # Zustand auth
    │   └── types/
    └── package.json
```

## Tài khoản demo (seed tự động)

| Email | Password | Role |
|-------|----------|------|
| admin@warranty.local | Admin@123 | Admin |
| manager@warranty.local | Manager@123 | Manager |
| reception@warranty.local | Receptionist@123 | Receptionist |
| tech01@warranty.local | Technician@123 | Technician |
| customer01@warranty.local | Customer@123 | Customer |

## Chạy Backend

### Yêu cầu
- .NET 10 SDK
- SQL Server (LocalDB / SQLEXPRESS / Docker)

### Cấu hình

File `appsettings.Development.json` đã có `Jwt:SigningKey`.

Connection string mặc định:

```
Server=.\SQLEXPRESS;Database=Warranty;Trusted_Connection=True;TrustServerCertificate=True;
```

Có thể đổi qua User Secrets hoặc biến môi trường:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Warranty;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:SigningKey" "your-secret-key-at-least-32-characters-long"
```

### Chạy

```bash
cd Electronic_device_warranty-main
dotnet restore
dotnet run --urls "http://localhost:5000"
```

API + Swagger: http://localhost:5000/swagger

Migration + seed chạy tự động khi start.

## Chạy Frontend

```bash
cd frontend
npm install
npm run dev
```

Mở http://localhost:5173

API URL mặc định: `http://localhost:5000` (có thể set `VITE_API_URL`).

## API chính

| Method | Route | Quyền |
|--------|-------|-------|
| POST | /api/auth/register | Anonymous (Customer) |
| POST | /api/auth/login | Anonymous |
| POST | /api/auth/refresh | Anonymous |
| GET/PUT/DELETE | /api/users/me | Authenticated |
| CRUD | /api/users | Admin |
| GET | /api/users/customers | Admin, Receptionist |
| GET | /api/users/technicians | Admin, Manager, Receptionist |
| CRUD | /api/products | Read: all auth; Write: Admin |
| GET/POST | /api/warranty-cards | Scope theo role |
| GET/POST | /api/repair-requests | Scope theo role |
| PUT | /api/repair-requests/{id}/technician | Receptionist, Manager |
| PUT | /api/repair-requests/{id}/status | Assigned Technician |
| GET | /api/reports/summary | Admin, Manager |
| CRUD | /api/categories | Admin write |
| CRUD | /api/parts | Staff read; Admin/Manager write |
| CRUD | /api/service-centers | Admin/Manager |

## Luồng nghiệp vụ

1. **Receptionist** tạo WarrantyCard gắn Product + Customer.
2. **Customer** hoặc Receptionist tạo RepairRequest (chỉ khi BH còn Active + chưa hết hạn).
3. **Receptionist/Manager** gán Technician.
4. **Technician** cập nhật Status (Received → InProgress → Completed → Returned).
5. Mỗi lần đổi status ghi vào RepairRequestStatusHistory.

## Đã bổ sung so với repo gốc

- CORS cho frontend React
- Controllers: Categories, Parts, ServiceCenters
- Frontend React đầy đủ 5 role
- Seed data thực tế (tên Việt, sản phẩm Dell/Apple/Samsung…)
- Refresh token auto-rotate trên frontend
- UI responsive (Tailwind)

## Ghi chú

- Models đã có Invoice, Payment, Notification, Feedback, Attachment — có thể mở rộng controller sau.
- Password hash: BCrypt, tối đa 72 byte.
- Soft-delete: IsActive = false, không hard-delete.
