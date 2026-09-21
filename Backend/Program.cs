using Microsoft.EntityFrameworkCore;
using EWarrantySystem.Data; // Hoặc Backend.Data (thay bằng namespace thực tế trong file AppDbContext.cs của bạn)

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký DbContext (nếu dùng InMemory)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("WarrantyDb"));

var app = builder.Build();

// 2. Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// 3. BẮT BUỘC PHẢI CÓ DÒNG NÀY Ở CUỐI FILE
app.Run();