using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using EWarrantySystem.Models;
using Microsoft.OpenApi;
using System.Text;
using EWarrantySystem.Data;
using EWarrantySystem.Services;
using EWarrantySystem.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/ewarranty-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // 1. Đăng ký dịch vụ
    builder.Services.AddControllers(options =>
    {
        var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        options.Filters.Add(
            new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IRepairRequestService, RepairRequestService>();
    builder.Services.AddScoped<IWarrantyCardService, WarrantyCardService>();

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<RepairRequestCreateDtoValidator>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddHostedService<WarrantyExpiryAlertService>();

    // Swagger hỗ trợ JWT Bearer
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "EWarrantySystem API", Version = "v1" });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Nhập JWT token theo dạng: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        c.AddSecurityDefinition("Bearer", securityScheme);

        // Sửa thành lambda function: _ => new OpenApiSecurityRequirement
        c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer"),
                new List<string>()
            }
        });
    });

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ===== JWT Authentication =====
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key is missing in configuration");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"];
    var jwtAudience = builder.Configuration["Jwt:Audience"];

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddScoped<EWarrantySystem.Services.JwtTokenService>();

    // ===== CORS =====
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000",   // React / Next dev
                    "http://localhost:5173",   // Vite
                    "http://localhost:4200"    // Angular (nếu dùng)
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();
    app.UseSerilogRequestLogging(); // log mỗi HTTP request

    // Global exception handling — phải đứng trước các middleware khác
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            var error = exceptionFeature?.Error;

            // Production: không lộ stack trace
            var message = app.Environment.IsDevelopment()
                ? error?.ToString()
                : "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";

            await context.Response.WriteAsJsonAsync(new
            {
                status = 500,
                message
            });
        });
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseCors("AllowFrontend");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ứng dụng khởi động thất bại");
}
finally
{
    Log.CloseAndFlush();
}
