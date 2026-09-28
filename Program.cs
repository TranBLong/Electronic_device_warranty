using Microsoft.EntityFrameworkCore;
using Warranty.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ Controllers
builder.Services.AddControllers();

// 2. Đăng ký DbContext với SQL Server từ appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Đăng ký dịch vụ Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Cấu hình Middleware cho Swagger trong môi trường Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();