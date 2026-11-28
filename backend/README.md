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

## Development demo accounts

In Development, `DatabaseSeeder` inserts the following accounts when the `Users` table is empty:

| Email | Password | Role |
|---|---|---|
| `admin@warranty.local` | `Admin@123` | Admin |
| `manager@warranty.local` | `Manager@123` | Manager |
| `reception@warranty.local` | `Receptionist@123` | Receptionist |
| `tech01@warranty.local` | `Technician@123` | Technician |
| `customer01@warranty.local` | `Customer@123` | Customer |

Passwords are hashed with BCrypt. The demo credentials are for local development only; production does not run the seeder. To provision a production administrator, use [`scripts/bootstrap-admin.sql`](./scripts/bootstrap-admin.sql) with a locally generated BCrypt hash and a separately chosen password. Never commit plaintext passwords, hashes, or live tokens.

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
| POST | `/api/repair-requests/{id}/attachments` | Multipart `file`; owner Customer, assigned Technician, Admin, Manager, or Receptionist |
| GET | `/api/repair-requests/{id}/attachments` | Same visibility rules as repair requests |
| GET | `/api/repair-requests/{id}/attachments/{attachmentId}/download` | Same visibility rules as repair requests |
| GET | `/api/reports/summary` | Admin or Manager |

Customer registration cannot choose a role. Profile updates accept only FullName, Email, and Phone. Account deactivation sets `IsActive = false`, revokes refresh tokens, blocks future authenticated requests, and preserves referenced records.

Repair requests created by a Customer have a null `ReceptionistId`; receptionist-created requests store the authenticated Receptionist. Warranty eligibility is checked using both persisted status and end date. Every request creation records its initial `Received` state, and every subsequent status change adds a history row in the same save operation.

Repair request attachments are uploaded as `multipart/form-data` using the `file` field. Allowed extensions and content types are configured under `Upload`; JPEG, PNG, and PDF uploads are also checked against their file signatures.

## Implemented scope

The API currently includes users and authentication, products and categories, warranty cards, repair requests and status history, repair parts and attachments, service centers, invoices and payments, notifications, feedback, and dashboard reporting. EF Core migrations are applied automatically at startup; keep existing migration files (including historical or empty migrations) if a database may already have applied them. Add future schema changes as new migrations.
