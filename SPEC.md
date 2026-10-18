# SPEC.md — Đặc tả nghiệp vụ E-Warranty System

## Đề tài
Đề tài 13: Thiết kế Back-end cho hệ thống Quản lý bảo hành thiết bị điện tử (E-Warranty System).

## Phân tầng phạm vi (khớp với thư mục `Models/`)
| Tầng | Thư mục | Entity | Trạng thái |
|---|---|---|---|
| Important | `Models/Important` | User, Product, WarrantyCard, RepairRequest, RepairRequestStatusHistory, RefreshToken | ✅ Xong: Models, Dtos, Repositories, Services, Controllers |
| Worth | `Models/Worth` | Category, Part, RepairRequestPart | ✅ Xong: Models, Dtos, Repositories, Services, Controllers, Migration, Tests |
| Complete | `Models/Complete` | ServiceCenter, Invoice, Payment, Notification, Feedback, RepairRequestAttachment | ✅ Xong: Models, Dtos, Repositories, Services, Controllers, Migration, Tests |

Quy ước:
- Hoàn thành xong tầng trước mới làm tầng sau. Entity của tầng sau được phép tham chiếu entity tầng trước, **không** ngược lại (trừ các FK nullable được ghi rõ bên dưới). `User.ServiceCenterId` là field seed-only, chưa có API để gán staff vào ServiceCenter.
- "Hoàn thành một tầng" nghĩa là đủ **cả 5 lớp** cho mọi entity của tầng đó: Model → Dto → Repository → Service → Controller (xem mục Kiến trúc phân lớp).
- Khi làm Worth/Complete, phần sửa vào entity/service của tầng trước (ví dụ thêm `Product.CategoryId`, `RepairRequest.ServiceCenterId`, thông báo khi đổi trạng thái) phải giữ tương thích ngược: field mới nullable, không làm hỏng API đã có.

## Kiến trúc phân lớp
Mọi tầng đều theo cùng một luồng: `Controller → Service → Repository → DbContext`.

| Lớp | Thư mục | Trách nhiệm | Không được làm |
|---|---|---|---|
| Controller | `Controllers/` | Nhận request, kiểm tra `[Authorize(Roles=...)]`, gọi Service, trả HTTP status | Chứa logic nghiệp vụ, truy cập DbContext |
| Service | `Services/` | Toàn bộ quy tắc nghiệp vụ (validation, phân quyền theo chủ sở hữu, đổi trạng thái, trừ kho...), ném exception nghiệp vụ | Trả entity thẳng ra ngoài (dùng Dto), tự viết truy vấn EF |
| Repository | `Repositories/` | Truy vấn/lưu dữ liệu bằng EF Core + LINQ (CRUD, lọc, phân trang, Include) | Chứa quy tắc nghiệp vụ |
| Dto | `Dtos/` | Request/Response, tách khỏi entity | — |
| Enum | `Enums/` | Role, WarrantyStatus, RepairStatus, InvoiceStatus, PaymentMethod... | — |
| Exception | `Exceptions/` | Exception nghiệp vụ (NotFound, Validation, Forbidden, Conflict...) | — |
| Middleware | `Middlewares/` | Bắt exception → trả JSON lỗi thống nhất | — |

Ghi chú:
- Controller chỉ gọi Service; Service chỉ gọi Repository (và Service khác nếu cần). Service **không** dùng `DbContext` trực tiếp.
- Thao tác ghi nhiều bước phải nằm trong 1 transaction ở tầng Service/Repository (ví dụ: đổi trạng thái + ghi `RepairRequestStatusHistory`; thêm `RepairRequestPart` + trừ `Part.StockQuantity`; ghi `Payment` + cập nhật `Invoice.Status`).
- Lỗi nghiệp vụ → ném exception trong `Exceptions/`; Middleware chuyển thành HTTP status tương ứng (404, 400, 403, 409).
- Dependency Injection: mỗi Repository/Service đăng ký trong `Program.cs`.
- Kiểm thử đặt trong project `Warranty.Tests` (ưu tiên test Service cho các quy tắc nghiệp vụ).

## Vai trò và quyền hạn
| Vai trò | Quyền |
|---|---|
| Admin | Quản trị toàn hệ thống: CRUD User, xem toàn bộ dữ liệu |
| Manager | Xem báo cáo/thống kê, quản lý trung tâm bảo hành (ServiceCenter), quản lý kho Part |
| Receptionist | Tạo WarrantyCard, tạo RepairRequest, gán Technician, lập Invoice, ghi nhận Payment |
| Technician | Xem RepairRequest được giao, cập nhật trạng thái sửa chữa, ghi linh kiện đã dùng (RepairRequestPart), tải đính kèm |
| Customer | Xem sản phẩm và phiếu bảo hành của chính mình, theo dõi tiến độ sửa chữa, xem hóa đơn, gửi Feedback, nhận Notification |

---

# TẦNG 1 — IMPORTANT (lõi) ✅

## Entity và trường dữ liệu

### User
- Id (PK), FullName, Email (unique), PasswordHash, Phone, Role (enum), IsActive, CreatedAt, ServiceCenterId (FK nullable; seed-only, chưa có API)

### Product
- Id (PK), Name, Brand, Model, SerialNumber (unique), WarrantyMonths, CreatedAt
- *(Tầng Worth bổ sung)* CategoryId (FK → Category, nullable; API Product hỗ trợ đọc/ghi)
- `ProductRequest.CategoryId` là tùy chọn; `null` nghĩa là chưa phân loại, giá trị khác null phải tham chiếu Category tồn tại. `ProductResponse` luôn trả CategoryId hiện tại.

### WarrantyCard
- Id (PK), ProductId (FK → Product), CustomerId (FK → User, Role=Customer)
- StartDate, EndDate, Status (enum: Active, Expired, Voided)

### RepairRequest
- Id (PK), WarrantyCardId (FK → WarrantyCard)
- ReceptionistId (FK → User), TechnicianId (FK → User, nullable — chưa gán)
- Description, Status (enum: Received, InProgress, Completed, Returned, Cancelled)
- IsChargeable, LaborCost (decimal >= 0; nếu IsChargeable=false thì LaborCost=0)
- CreatedAt, UpdatedAt
- *(Tầng Complete bổ sung)* ServiceCenterId (FK → ServiceCenter, nullable)

### RepairRequestStatusHistory
- Id (PK), RepairRequestId (FK), OldStatus, NewStatus, ChangedBy (FK → User), ChangedAt

### RefreshToken
- Id (PK), UserId (FK → User), Token, ExpiresAt, IsRevoked, CreatedAt

## Luồng nghiệp vụ chính
1. **Đăng ký bảo hành**: Receptionist tạo `WarrantyCard` gắn với `Product` và `Customer`.
2. **Yêu cầu sửa chữa**: Customer hoặc Receptionist tạo `RepairRequest` từ một `WarrantyCard` còn hiệu lực (`Status = Active` và `EndDate >= hôm nay`).
3. **Phân công**: Receptionist hoặc Manager gán `TechnicianId` cho `RepairRequest`.
4. **Cập nhật tiến độ**: Technician được giao chuyển trạng thái theo bảng transition bên dưới; Receptionist được chuyển `Completed` → `Returned`. Mỗi lần đổi ghi 1 dòng vào `RepairRequestStatusHistory`.
5. **Đánh giá chi phí**: Technician được giao hoặc Manager đặt `IsChargeable` và `LaborCost` khi request `InProgress`/`Completed` và chưa có invoice. `LaborCost >= 0`; nếu `IsChargeable=false` thì `LaborCost=0`.
6. **Hoàn tất**: Status chuyển `Completed` → `Returned` khi trả máy cho khách.

### Chuyển trạng thái RepairRequest

| Từ | Đến hợp lệ | Ai được chuyển | Hệ quả |
|---|---|---|---|
| (chưa có) | `Received` | Customer hoặc Receptionist khi tạo request | Ghi history ban đầu; không tạo notification |
| `Received` | `InProgress` hoặc `Cancelled` | Technician được giao | Ghi history và tạo notification cho Customer; hủy không hoàn kho |
| `InProgress` | `Completed` hoặc `Cancelled` | Technician được giao | Ghi history và tạo notification cho Customer; hủy không hoàn kho |
| `Completed` | `Returned` | Technician được giao hoặc Receptionist | Ghi history và tạo notification cho Customer |
| `Returned` | — | Không ai | Trạng thái kết thúc |
| `Cancelled` | — | Không ai | Trạng thái kết thúc; không hoàn kho |

Mọi chuyển trạng thái không có trong bảng đều bị từ chối. Manager không trực tiếp chuyển trạng thái.

## Quy tắc nghiệp vụ (validation quan trọng)
- Không tạo `RepairRequest` nếu `WarrantyCard.Status != Active` hoặc đã hết hạn.
- `TechnicianId` phải là User có `Role = Technician`.
- `CustomerId` trong `WarrantyCard` phải là User có `Role = Customer`.
- Customer chỉ được xem `WarrantyCard`/`RepairRequest` mà `CustomerId` là chính họ.
- Technician chỉ được cập nhật `RepairRequest` mà `TechnicianId` là chính họ.
- Chỉ Technician được giao hoặc Manager được đặt `IsChargeable`/`LaborCost`, chỉ khi request `InProgress` hoặc `Completed` và chưa có invoice.
- Hủy RepairRequest không hoàn lại `Part.StockQuantity` của linh kiện đã ghi nhận sử dụng.

## Xác thực (JWT)
- Đăng nhập bằng Email + Password → trả Access Token (hết hạn 30 phút) + Refresh Token (lưu DB, hết hạn dài hơn, ví dụ 7 ngày).
- Endpoint `/api/auth/refresh` dùng Refresh Token còn hiệu lực để cấp Access Token mới, xoay vòng (rotate) Refresh Token cũ.
- Payload JWT chứa: `sub` (UserId), `role`, `email`.
- Mật khẩu băm bằng BCrypt.

---

# TẦNG 2 — WORTH (quản lý linh kiện) 🔶

## Entity và trường dữ liệu

### Category
- Id (PK), Name (unique), Description, IsActive, CreatedAt

### Part
- Id (PK), CategoryId (FK → Category), Name, PartNumber (unique), Description, UnitPrice, StockQuantity, IsActive, CreatedAt

### RepairRequestPart
- Id (PK), RepairRequestId (FK → RepairRequest), PartId (FK → Part)
- Quantity, UnitPrice (lưu giá tại thời điểm dùng), UsedAt

## Luồng nghiệp vụ
6. **Quản lý kho**: Manager tạo `Category` và `Part`, cập nhật `StockQuantity`.
7. **Dùng linh kiện**: Technician thêm `RepairRequestPart` vào `RepairRequest` được giao; hệ thống trừ `Part.StockQuantity` tương ứng.

## Quy tắc nghiệp vụ
- `Quantity > 0` và `Quantity <= Part.StockQuantity`; nếu không đủ tồn kho thì từ chối.
- Chỉ thêm linh kiện khi `RepairRequest.Status = InProgress` và `TechnicianId` là chính Technician đang thao tác.
- `UnitPrice` sao chép từ `Part.UnitPrice` lúc thêm, đổi giá sau này không ảnh hưởng dữ liệu cũ.
- Không được thêm Part đã ngừng hoạt động (`IsActive=false`) vào RepairRequest.
- `PartUpdateDto.IsActive` là tùy chọn để tương thích client cũ; khi được gửi, Manager/Admin có thể bật hoặc ngừng hoạt động Part.
- Không được thêm RepairRequestPart nếu RepairRequest đã có invoice.
- Không xóa `Category` đang có `Part`; không xóa `Part` đã được dùng trong `RepairRequestPart`.

## Các lớp cần làm (Worth)
| Entity | Repository | Service | Controller | Quyền gọi |
|---|---|---|---|---|
| Category | CategoryRepository | CategoryService | CategoriesController | Manager/Admin ghi; mọi vai trò đã đăng nhập được đọc |
| Part | PartRepository | PartService | PartsController | Manager/Admin ghi; Receptionist/Technician đọc |
| RepairRequestPart | RepairRequestPartRepository | RepairRequestPartService | RepairRequestPartsController (route lồng `/api/repair-requests/{id}/parts`) | Technician (được giao) thêm; Admin/Manager/Receptionist/Customer (chủ) đọc |

Lưu ý riêng của tầng:
- Thêm `RepairRequestPart` và trừ `Part.StockQuantity` phải trong **cùng 1 transaction**.
- `Product.CategoryId` đã được thêm nullable bởi migration `AddCategoryAndParts`; GĐ4 chỉ hoàn thiện DTO, validation và service cho field này, không cần migration mới.

---

# TẦNG 3 — COMPLETE (hoàn thiện) 🔶

## Entity và trường dữ liệu

### ServiceCenter
- Id (PK), Name, Address, Phone, ManagerId (FK → User, Role=Manager), IsActive, CreatedAt

### Invoice
- Id (PK), RepairRequestId (FK → RepairRequest), InvoiceNumber (unique), PartsTotal, LaborTotal, TotalAmount
- Status (enum: Unpaid, Paid, Cancelled), IssuedAt, IssuedById (FK → User), PaidAt (nullable)
- CancelledAt (nullable), CancelledById (FK → User, nullable), CancellationReason (nullable)

### Payment
- Id (PK), InvoiceId (FK → Invoice), Amount
- Method (enum: Cash, Transfer, Card), Note (nullable), PaidAt, ReceivedById (FK → User)

### Notification
- Id (PK), UserId (FK → User), RepairRequestId (FK nullable), Type, Title, Message, IsRead, CreatedAt

### Feedback
- Id (PK), RepairRequestId (FK → RepairRequest, unique), CustomerId (FK → User, Role=Customer)
- Rating (1–5), Comment, CreatedAt

### RepairRequestAttachment
- Id (PK), RepairRequestId (FK → RepairRequest)
- FileName, FilePath (đường dẫn tương đối), ContentType, FileSize, UploadedById (FK → User), UploadedAt

## Luồng nghiệp vụ
8. **Trung tâm bảo hành**: Manager quản lý `ServiceCenter`; `RepairRequest` có thể gắn với một trung tâm.
9. **Lập hóa đơn**: Khi `RepairRequest` đạt `Completed` hoặc `Returned`, Receptionist lập invoice từ dữ liệu DB. `PartsTotal = SUM(RepairRequestPart.Quantity * UnitPrice)`; `LaborTotal = RepairRequest.LaborCost`; `TotalAmount = RepairRequest.IsChargeable ? PartsTotal + LaborTotal : 0`. Client không gửi/định đoạt giá hay trạng thái tính phí. Invoice 0 đồng tự chuyển `Paid`, đặt `PaidAt`, không tạo Payment.
10. **Thanh toán**: Receptionist ghi nhận `Payment` từng phần. Từ chối payment lớn hơn số dư hiện tại (`TotalAmount - SUM(Payment.Amount)`). Khi thanh toán đủ, đặt invoice `Paid`/`PaidAt`.
11. **Thông báo**: Tạo notification cho Customer khi RepairRequest đổi trạng thái, khi lập invoice, khi invoice được thanh toán đủ, và khi invoice bị hủy.
12. **Đánh giá**: Sau khi `Returned`, Customer gửi `Feedback` (1 lần cho mỗi `RepairRequest`).
13. **Đính kèm**: Customer/Receptionist/Technician tải ảnh hoặc tài liệu liên quan lên `RepairRequest`.

## Quy tắc nghiệp vụ
- Chỉ lập `Invoice` khi `RepairRequest.Status` là `Completed` hoặc `Returned`; mỗi RepairRequest có tối đa một invoice chưa hủy. Invoice `Cancelled` được giữ làm lịch sử và không chặn tạo invoice thay thế; unique index chỉ áp dụng cho invoice không `Cancelled`.
- Invoice lấy `PartsTotal` từ RepairRequestPart và `LaborTotal` từ RepairRequest; không tin dữ liệu tính tiền do client gửi.
- Chỉ hủy invoice chưa thanh toán và chưa có Payment. Hủy lưu thời điểm, actor và lý do tùy chọn; body không có lý do vẫn hợp lệ.
- Không ghi `Payment` cho `Invoice` đã `Paid` hoặc `Cancelled`; `Payment.Amount > 0` và không vượt số dư.
- Customer chỉ xem `Invoice`, `Payment`, `Notification`, `Attachment` thuộc `RepairRequest`/tài khoản của chính mình.
- `Feedback` chỉ hợp lệ khi `RepairRequest.Status = Returned` và `CustomerId` khớp chủ của `WarrantyCard`.
- `Notification` chỉ được đánh dấu đã đọc bởi chính `UserId` sở hữu.
- `ManagerId` của `ServiceCenter` phải là User có `Role = Manager`.
- Giới hạn `Attachment`: định dạng ảnh/PDF, dung lượng tối đa (ví dụ 5 MB/file).

## Các lớp cần làm (Complete)
| Entity | Repository | Service | Controller | Quyền gọi |
|---|---|---|---|---|
| ServiceCenter | ServiceCenterRepository | ServiceCenterService | ServiceCentersController | Manager/Admin ghi; vai trò khác đọc |
| Invoice | InvoiceRepository | InvoiceService | InvoicesController | Receptionist lập; Customer (chủ) xem; Admin/Manager xem tất cả |
| Payment | PaymentRepository | PaymentService | PaymentsController | Receptionist ghi; Customer (chủ) xem |
| Notification | NotificationRepository | NotificationService | NotificationsController | Chỉ chủ tài khoản đọc/đánh dấu đã đọc |
| Feedback | FeedbackRepository | FeedbackService | FeedbacksController | Customer (chủ) gửi; Admin/Manager xem |
| RepairRequestAttachment | RepairRequestAttachmentRepository | RepairRequestAttachmentService | RepairRequestAttachmentsController | Customer (chủ)/Receptionist/Technician (được giao) tải lên |

Lưu ý riêng của tầng:
- Tạo `Notification` cho thay đổi trạng thái, lập invoice, thanh toán đủ và hủy invoice trong cùng transaction với thao tác ghi liên quan.
- Ghi `Payment` + cập nhật `Invoice.Status` + notification (nếu thanh toán đủ) phải trong **cùng 1 transaction**.
- `RepairRequest.ServiceCenterId` là nullable; schema hiện có các bảng Complete và migration `AddCompleteTier` bổ sung các FK bắt buộc cho Manager/Invoice issuer cùng chính sách xóa an toàn.
- Hóa đơn 0 đồng tự chuyển sang `Paid`; không tạo Payment cho hóa đơn này.
- Upload file: chỉ nhận JPG/JPEG/PNG/PDF, kiểm tra signature, giới hạn dung lượng và thư mục lưu được cấu hình trong `Upload`; file nằm ngoài `wwwroot` và DB chỉ lưu đường dẫn tương đối.

---

## Phạm vi hiện tại
Ba tầng ở trên (Important → Worth → Complete) nằm trong phạm vi dự án, làm lần lượt theo thứ tự tầng. Trạng thái cụ thể xem bảng "Phân tầng phạm vi".
Phần sau **chưa làm** trong giai đoạn này: AuditLog.