# EWarrantySystem - Hệ Thống Quản Lý Bảo Hành Thiết Bị Điện Tử

Hệ thống Backend Web API quản lý quy trình bảo hành, tiếp nhận sự cố sửa chữa thiết bị điện tử, phân công kỹ thuật viên và thông báo tự động.

## 🛠 Công nghệ sử dụng
- **Framework:** .NET (ASP.NET Core Web API)
- **Database:** SQL Server, Entity Framework Core
- **Authentication & Authorization:** JWT Bearer Token, Role-based
- **Validation & Logging:** FluentValidation, Serilog
- **Testing:** xUnit, Moq, EF Core InMemory
- **Background Service:** Hosted Service tự động quét và gửi mail nhắc nhở sắp hết hạn bảo hành

## 📁 Cấu trúc thư mục
```text
Electronic_device/
├── backend/                  # Mã nguồn API Backend chính
│   ├── Controllers/          # Các API Endpoints
│   ├── Services/             # Xử lý nghiệp vụ logic
│   ├── Models/               # Định nghĩa các thực thể EF
│   ├── DTOs/                 # Data Transfer Objects
│   └── Data/                 # AppDbContext & Config Database
└── EWarrantySystem.Tests/    # Dự án Unit Test tự động (xUnit)