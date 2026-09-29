# E-Warranty API

ASP.NET Core .NET 10 API backed by SQL Server and EF Core.

## Configuration and database

Configure `ConnectionStrings:DefaultConnection` and `Jwt:SigningKey` through environment variables or .NET User Secrets. The signing key must contain at least 32 UTF-8 bytes; do not put it in a committed settings file.

Example local setup:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<a-random-secret-at-least-32-bytes>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=Warranty;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Environment variable equivalents are `Jwt__SigningKey` and `ConnectionStrings__DefaultConnection`. JWT issuer and audience are configured in `appsettings.json`; access tokens expire after 30 minutes and refresh tokens after 7 days. Passwords must be at least 8 characters and no more than 72 UTF-8 bytes because BCrypt has a 72-byte input limit.

## First Admin account

There is no default account or automatic privileged-user seed. After applying migrations, generate a BCrypt password hash locally with `BCrypt.Net.BCrypt.HashPassword(...)`, replace both placeholders in [`scripts/bootstrap-admin.sql`](./scripts/bootstrap-admin.sql), and execute that script against the `Warranty` database. Never store the plaintext password in the script or source control. The `Role` database value is `Admin`.

## API routes

All routes other than registration, login, and refresh require a valid bearer access token.

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | Anonymous; creates Customer only |
| POST | `/api/auth/login` | Anonymous |
| POST | `/api/auth/refresh` | Anonymous; rotates refresh token |
| GET/PUT/DELETE | `/api/users/me` | Authenticated self-service; DELETE deactivates the account |
| GET/POST/PUT/DELETE | `/api/users`, `/api/users/{id}` | Admin; DELETE deactivates, never hard-deletes |
| GET | `/api/users/customers` | Admin or Receptionist; active Customer accounts |
| GET | `/api/users/technicians` | Admin, Manager, or Receptionist; active Technician accounts |
| GET/POST/PUT/DELETE | `/api/products`, `/api/products/{id}` | Authenticated read; Admin write |
| GET/POST | `/api/warranty-cards` | Customer sees own cards; Admin/Receptionist see all; Receptionist creates |
| GET/POST | `/api/repair-requests` | Customer/Technician scoped to own records; Admin/Manager/Receptionist see all; Customer or Receptionist creates |
| PUT | `/api/repair-requests/{id}/technician` | Receptionist or Manager |
| PUT | `/api/repair-requests/{id}/status` | Assigned Technician |
| GET | `/api/reports/summary` | Admin or Manager |

Customer registration cannot choose a role. Profile updates accept only FullName, Email, and Phone. Account deactivation sets `IsActive = false`, revokes refresh tokens, blocks future authenticated requests, and preserves referenced records.

Repair requests created by a Customer have a null `ReceptionistId`; receptionist-created requests store the authenticated Receptionist. Warranty eligibility is checked using both persisted status and end date. Every request creation records its initial `Received` state, and every subsequent status change adds a history row in the same save operation.

## Scope

The API implements User, Product, WarrantyCard, RepairRequest, RepairRequestStatusHistory, and RefreshToken. ServiceCenter, SparePart, Invoice/Payment, Notification, Feedback, and AuditLog are not included.
