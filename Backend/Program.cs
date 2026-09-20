var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các dịch vụ
builder.Services.AddControllers(); // <-- [BẮT BUỘC] Thêm dòng này để nhận diện UsersController
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Cấu hình Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 3. Định tuyến Controller
app.MapControllers(); // <-- [BẮT BUỘC] Thêm dòng này để ánh xạ các API trong Controller

app.Run();