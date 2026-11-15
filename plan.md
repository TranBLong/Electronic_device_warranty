# Kế hoạch hoàn thiện yêu cầu phi chức năng

Tài liệu này theo dõi các khoảng trống được đối chiếu từ mã nguồn. Trong lượt cập nhật tài liệu, không thay đổi file `.cs`.

## 1. Chuẩn hóa lỗi và logging
- [ ] Cập nhật `ExceptionHandlingMiddleware`: giữ ánh xạ exception nghiệp vụ hiện có sang `ProblemDetails`; thêm catch-all 500, `traceId`, thông điệp chung và không lộ nội dung exception ở Production.
- [ ] Thêm `ILogger<T>` cho middleware và các Service cần thiết; chọn mức log phù hợp và không ghi mật khẩu, token hoặc dữ liệu nhạy cảm.
- [ ] Kiểm tra mọi đường lỗi vẫn để exception nghiệp vụ đi qua middleware; không biến lỗi nghiệp vụ thành 500.

## 2. Thống nhất danh sách và phân trang
- [ ] Bổ sung `QueryParameters` với `PageNumber`, `PageSize`, `Keyword`, `SortBy`, `IsDescending`; xác định và áp dụng giới hạn PageSize tối đa (**CẦN XÁC NHẬN giá trị**).
- [ ] Bổ sung `TotalPages` cho `PagedResponseDto<T>` và thống nhất DTO phản hồi danh sách đang phân trang.
- [ ] Hoàn thiện paging cho `GET /api/products`, `/api/users`, `/api/users/customers`, `/api/users/technicians`, `/api/warranty-cards`, `/api/repair-requests`, `/api/categories`, `/api/service-centers`.
- [ ] Chuyển các endpoint đã paging (`/api/parts`, `/api/feedbacks`, `/api/invoices`, `/api/invoices/my`, `/api/notifications`) sang query class chung.
- [ ] Thêm tìm kiếm/sắp xếp động theo từng danh sách; chỉ cho phép các field đã được whitelist, giữ bộ lọc và giới hạn quyền truy cập dữ liệu hiện tại.
- [ ] Lập danh sách sort field được phép cho từng endpoint trước khi triển khai. Các cột sắp xếp cố định trong SPEC là hiện trạng, không tự hiểu là dynamic sort đã được hỗ trợ.

## 3. Cấu hình, CORS, health và đóng gói
- [ ] Tạo Options class và đăng ký/bind/validate cấu hình upload; chuyển Service khỏi việc đọc `IConfiguration` trực tiếp.
- [ ] Thêm cấu hình CORS trong appsettings, đăng ký middleware đúng thứ tự; origin triển khai: **CẦN XÁC NHẬN**.
- [ ] Đăng ký health checks và endpoint `/health`; xác định kiểm tra tối thiểu cần có (**CẦN XÁC NHẬN**).
- [ ] Tạo Dockerfile phù hợp với cấu trúc solution và cách cấp cấu hình bí mật lúc chạy.
- [ ] Đưa signing key ra khỏi `appsettings.Development.json`; dùng .NET User Secrets hoặc biến môi trường. Bổ sung quy tắc loại trừ file chứa bí mật vào `.gitignore` khi thích hợp; kiểm tra file đã được track để tránh hiểu nhầm rằng `.gitignore` tự gỡ khỏi Git.

## 4. Thứ tự thực hiện và kiểm tra
1. Thiết kế chung: ProblemDetails/logging, QueryParameters/paging, Options cho upload.
2. Áp dụng lần lượt cho các endpoint danh sách, giữ nguyên quyền và quy tắc nghiệp vụ.
3. Cấu hình CORS, health check, Dockerfile và hướng dẫn cấu hình bí mật.
4. Cập nhật SPEC theo hành vi đã triển khai; kiểm tra build và các test liên quan theo quy trình dự án.

## Giả định cần rà soát
- Giới hạn PageSize tối đa, whitelist field sort, origin CORS và phạm vi health check chưa có trong code nên chưa chốt giá trị.
- Cấu hình upload hiện tại trong `appsettings.json` được xem là giá trị mặc định đang chạy; triển khai vẫn phải cho phép override bằng cấu hình ngoài.
- Dockerfile là yêu cầu kế hoạch mới; chưa có Dockerfile hiện tại trong repo.
