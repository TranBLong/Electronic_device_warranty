# Backend guidance

## Current implementation

This .NET 10 ASP.NET Core API uses EF Core and SQL Server. Implemented areas include:

- Authentication, user profiles, roles, and refresh-token rotation.
- Products, categories, warranty cards, and repair requests, including status history, parts, and attachments.
- Service centers, invoices, payments, notifications, customer feedback, dashboard reports, and warranty-expiry notifications.

The EF model is defined by `Data/AppDbContext.cs`; API behavior is implemented in `Warranty/Controllers/Important/` and `Warranty/Services/`. Keep documentation aligned with the actual controllers, models, and database seeder. There is no AuditLog feature.

## Database changes

- Add schema changes as new EF Core migrations; do not edit or remove migrations already applied to a database.
- Keep development seed accounts and passwords in sync with `Data/DatabaseSeeder.cs`.
- The baseline demo seed runs only in Development and only when the Users table is empty.
- Do not add credentials, password hashes, refresh tokens, or database dumps to source control.
