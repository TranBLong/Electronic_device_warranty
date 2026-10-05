# SPEC.md — Đặc tả nghiệp vụ E-Warranty System

## Đề tài
Đề tài 13: Thiết kế Back-end cho hệ thống Quản lý bảo hành thiết bị điện tử (E-Warranty System).

## Vai trò và quyền hạn
| Vai trò | Quyền |
|---|---|
| Admin | Quản trị toàn hệ thống: CRUD User, xem toàn bộ dữ liệu |
| Manager | Xem báo cáo/thống kê, quản lý trung tâm bảo hành |
| Receptionist | Tạo WarrantyCard, tạo RepairRequest, gán Technician |
| Technician | Xem RepairRequest được giao, cập nhật trạng thái sửa chữa |
| Customer | Xem sản phẩm và phiếu bảo hành của chính mình, theo dõi tiến độ sửa chữa |

## Entity và trường dữ liệu

### User
- Id (PK), FullName, Email (unique), PasswordHash, Phone, Role (enum), IsActive, CreatedAt

### Product
- Id (PK), Name, Brand, Model, SerialNumber (unique), WarrantyMonths, CreatedAt

### WarrantyCard
- Id (PK), ProductId (FK → Product), CustomerId (FK → User, Role=Customer)
- StartDate, EndDate, Status (enum: Active, Expired, Voided)

### RepairRequest
- Id (PK), WarrantyCardId (FK → WarrantyCard)
- ReceptionistId (FK → User), TechnicianId (FK → User, nullable — chưa gán)
- Description, Status (enum: Received, InProgress, Completed, Returned, Cancelled)
- CreatedAt, UpdatedAt

### RepairRequestStatusHistory
- Id (PK), RepairRequestId (FK), OldStatus, NewStatus, ChangedBy (FK → User), ChangedAt

### RefreshToken
- Id (PK), UserId (FK → User), Token, ExpiresAt, IsRevoked, CreatedAt

## Luồng nghiệp vụ chính
1. **Đăng ký bảo hành**: Receptionist tạo `WarrantyCard` gắn với `Product` và `Customer`.
2. **Yêu cầu sửa chữa**: Customer hoặc Receptionist tạo `RepairRequest` từ một `WarrantyCard` còn hiệu lực (`Status = Active` và `EndDate >= hôm nay`).
3. **Phân công**: Receptionist hoặc Manager gán `TechnicianId` cho `RepairRequest`.
4. **Cập nhật tiến độ**: Technician đổi `Status` của `RepairRequest`; mỗi lần đổi ghi 1 dòng vào `RepairRequestStatusHistory`.
5. **Hoàn tất**: Status chuyển `Completed` → `Returned` khi trả máy cho khách.

## Quy tắc nghiệp vụ (validation quan trọng)
- Không tạo `RepairRequest` nếu `WarrantyCard.Status != Active` hoặc đã hết hạn.
- `TechnicianId` phải là User có `Role = Technician`.
- `CustomerId` trong `WarrantyCard` phải là User có `Role = Customer`.
- Customer chỉ được xem `WarrantyCard`/`RepairRequest` mà `CustomerId` là chính họ.
- Technician chỉ được cập nhật `RepairRequest` mà `TechnicianId` là chính họ.

## Xác thực (JWT)
- Đăng nhập bằng Email + Password → trả Access Token (hết hạn 30 phút) + Refresh Token (lưu DB, hết hạn dài hơn, ví dụ 7 ngày).
- Endpoint `/api/auth/refresh` dùng Refresh Token còn hiệu lực để cấp Access Token mới, xoay vòng (rotate) Refresh Token cũ.
- Payload JWT chứa: `sub` (UserId), `role`, `email`.

## Phạm vi hiện tại
Chỉ các entity/luồng ở trên. Các phần sau **chưa làm** trong giai đoạn này:
ServiceCenter, SparePart, Invoice/Payment, Notification, Feedback, AuditLog.