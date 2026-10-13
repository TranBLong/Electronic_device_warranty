# AGENTS.md — Hướng dẫn cho AI agent (Copilot Agent mode / Claude)

Dự án: **E-Warranty System** — Back-end ASP.NET Core Web API + EF Core + SQL Server.
Đặc tả nghiệp vụ đầy đủ nằm trong `SPEC.md` — **đọc `SPEC.md` trước khi viết code**. File này chỉ quy định cách làm.

## Cấu trúc dự án
```
Warranty/                  (solution)
├─ scripts/                (script SQL)
├─ Warranty/               (project API)
│  ├─ Controllers/
│  ├─ Dtos/
│  ├─ Enums/
│  ├─ Exceptions/
│  ├─ Middlewares/
│  ├─ Models/
│  │  ├─ Important/        Tầng 1 — lõi
│  │  ├─ Worth/            Tầng 2 — quản lý linh kiện
│  │  └─ Complete/         Tầng 3 — hoàn thiện
│  ├─ Repositories/
│  ├─ Services/
│  ├─ appsettings.json / appsettings.Development.json
│  └─ Program.cs
└─ Warranty.Tests/         (unit test)
```

## Thứ tự làm việc (bắt buộc)
1. Làm theo tầng: **Important → Worth → Complete**. Xong tầng trước mới sang tầng sau.
2. Trong mỗi tầng, làm theo thứ tự lớp: **Model → Dto → Repository → Service → Controller → (Test)**.
3. Trạng thái hiện tại: Important đã xong đủ các lớp; Worth và Complete mới có Models. Cập nhật bảng trạng thái trong `SPEC.md` sau khi xong mỗi tầng.
4. Không làm phần ngoài phạm vi (ví dụ AuditLog) trừ khi được yêu cầu.

## Kiến trúc phân lớp
`Controller → Service → Repository → DbContext`

- **Controller**: mỏng. Chỉ nhận request, gắn `[Authorize(Roles = "...")]`, gọi Service, trả kết quả. Không chứa logic nghiệp vụ, không dùng `DbContext`.
- **Service**: chứa mọi quy tắc nghiệp vụ trong `SPEC.md` (validation, kiểm tra chủ sở hữu, đổi trạng thái, trừ kho, tạo thông báo...). Làm việc với Dto, không trả entity ra ngoài. Không dùng `DbContext` trực tiếp.
- **Repository**: chỉ truy vấn/lưu dữ liệu bằng EF Core + LINQ. Không chứa quy tắc nghiệp vụ.
- **Dto**: tách riêng Request và Response; không expose `PasswordHash`, `Token` hay navigation property vòng lặp.
- Thao tác ghi nhiều bước đặt trong 1 transaction (đổi trạng thái + ghi history; thêm linh kiện + trừ kho; ghi payment + cập nhật invoice).
- Đăng ký Repository/Service trong `Program.cs` bằng DI (interface `IXxxRepository`, `IXxxService`).

## Quy ước code
- **Tên**: PascalCase cho class/method/property; interface bắt đầu bằng `I`; Service/Repository/Controller đặt theo dạng `XxxService`, `XxxRepository`, `XxxsController`; method async có hậu tố `Async`.
- **Làm giống code đã có ở tầng Important** (đặt tên, cách tổ chức Dto, cách trả lỗi) — mở file tương ứng của Important để làm mẫu trước khi tạo file mới.
- Dùng `async/await` và `CancellationToken` ở các method truy cập DB nếu code hiện tại đã dùng.
- Truy vấn đọc dùng `AsNoTracking()`; chỉ `Include` khi cần.
- Không hard-code secret/connection string; lấy từ `appsettings*.json`.
- Lỗi nghiệp vụ ném exception trong `Exceptions/`, **không** trả `500`; `Middlewares/` chuyển thành 400/403/404/409 với JSON thống nhất.
- Không đổi/phá API và schema của tầng đã hoàn thành. Field mới thêm vào entity cũ (`Product.CategoryId`, `RepairRequest.ServiceCenterId`) phải **nullable**.
- Thay đổi schema → tạo EF migration riêng cho mỗi tầng, đặt tên rõ nghĩa (ví dụ `AddWorthTier`, `AddCompleteTier`).

## Phân quyền
- Xác thực JWT; Access Token 30 phút, Refresh Token lưu DB (7 ngày), xoay vòng khi refresh. Mật khẩu băm BCrypt.
- Lấy UserId và Role từ claim (`sub`, `role`), không nhận từ body request.
- 5 vai trò: Admin, Manager, Receptionist, Technician, Customer. Bảng quyền và quy tắc "chỉ xem/sửa dữ liệu của chính mình" nằm trong `SPEC.md` — kiểm tra ở **Service**, không chỉ ở attribute `[Authorize]`.

## Checklist trước khi coi một tầng là xong
- [ ] Mọi entity của tầng có đủ Dto, Repository, Service, Controller và đã đăng ký DI.
- [ ] Mọi quy tắc nghiệp vụ trong mục "Quy tắc nghiệp vụ" của tầng được kiểm tra trong Service.
- [ ] Endpoint có `[Authorize(Roles = ...)]` đúng bảng quyền.
- [ ] Đã có migration và project build không lỗi/cảnh báo mới.
- [ ] Đã có test cho các quy tắc nghiệp vụ quan trọng trong `Warranty.Tests`.
- [ ] Đã cập nhật trạng thái tầng trong `SPEC.md`.