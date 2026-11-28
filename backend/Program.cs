using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Middlewares;
using Warranty.Options;
using Warranty.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey) && Encoding.UTF8.GetByteCount(o.SigningKey) >= 32,
        "Jwt:SigningKey must be at least 32 UTF-8 bytes")
    .ValidateOnStart();

builder.Services.AddOptions<UploadOptions>()
    .Bind(builder.Configuration.GetSection(UploadOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt section missing.");
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey required (>=32 bytes). Development: dotnet user-secrets set \"Jwt:SigningKey\" \"...\"");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection missing.")));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste access token only (no Bearer prefix).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<RepairRequestAttachmentService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHostedService<WarrantyExpiryBackgroundService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.AdminOrManager, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.Staff, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Receptionist));
    options.AddPolicy(Policies.InternalStaff, p =>
        p.RequireRole(Roles.Admin, Roles.Manager, Roles.Receptionist, Roles.Technician));
    options.AddPolicy(Policies.CanManageUsers, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.CanManageCatalog, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.CanManageServiceCenters, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.CanAssignTechnician, p =>
        p.RequireRole(Roles.Admin, Roles.Manager, Roles.Receptionist));
    options.AddPolicy(Policies.CanManageBilling, p =>
        p.RequireRole(Roles.Admin, Roles.Manager, Roles.Receptionist));
});

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000",
                "http://127.0.0.1:5173", "http://127.0.0.1:3000")
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await DatabaseSeeder.SeedAsync(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseMiddleware<Warranty.Middlewares.ActiveUserMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

public partial class Program;
