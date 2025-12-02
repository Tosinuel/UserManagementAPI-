# UserManagementAPI

This repository contains a simple ASP.NET Core Web API for managing users (create, read, update, delete). It was implemented in three phases: scaffold, debugging/improvements, and middleware for logging, error handling, and token authentication.

Quick start

- Requirements: .NET 7 SDK or later

Run locally (PowerShell):

```powershell
cd "c:\Users\expre\Documents\C# Programming\UserManagementAPI"
dotnet run
```

API endpoints

- GET `/api/users` : Get all users
- GET `/api/users/{id}` : Get user by ID
- POST `/api/users` : Create user (JSON body)
- PUT `/api/users/{id}` : Update user (JSON body)
- DELETE `/api/users/{id}` : Delete user

Authentication

All endpoints require a bearer token in the `Authorization` header for demo purposes.

Example header:

```
Authorization: Bearer secrettoken123
```

Middleware

- `ErrorHandlingMiddleware`: catches unhandled exceptions and returns `{"error":"Internal server error."}` with HTTP 500.
- `TokenAuthenticationMiddleware`: validates a demo token and returns 401 for invalid/missing tokens.
- `RequestResponseLoggingMiddleware`: logs the HTTP method, path, and response status code.

How Copilot helped

- Suggested structure for middleware and DI registration in `Program.cs`.
- Provided patterns for consistent error handling (global middleware) and logging.
- Helped compose controller actions with ModelState validation and proper status codes (e.g., `CreatedAtAction`, `NoContent`, `NotFound`).

Testing

Use Postman or curl. Example curl commands:

```powershell
# List users
curl -H "Authorization: Bearer secrettoken123" http://localhost:5000/api/users

# Create user
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer secrettoken123" -d '{"firstName":"Alice","lastName":"Smith","email":"alice@example.com"}' http://localhost:5000/api/users
```

Phase 2 notes (debugging & fixes)

- Validation: Added DataAnnotations to `User` and `ModelState` checks in the controller methods to prevent invalid user creation (empty names or invalid emails).
- Error handling: Added `ErrorHandlingMiddleware` to prevent crashes and return consistent JSON error responses.
- Minor performance: `InMemoryUserRepository.GetAllAsync` returns a snapshot list to avoid enumeration concurrency issues.

Phase 3 notes (middleware & security)

- Middleware implemented and configured in `Program.cs` in the required order: error handling -> authentication -> logging.

Next steps / recommended improvements

- Replace the demo token validation with a proper JWT validation using `Microsoft.AspNetCore.Authentication.JwtBearer` (implemented in this branch).
- Replace the `InMemoryUserRepository` with a real database (EF Core) for persistence and add paging for large data sets (EF Core + SQLite added).
- Add unit/integration tests.

EF Core and JWT details

- The project now includes `Microsoft.EntityFrameworkCore.Sqlite` and an `AppDbContext` that stores users in a local `users.db` SQLite file by default.
- On startup the app calls `EnsureCreated()` to create the SQLite DB file automatically. To change the DB location add a `ConnectionStrings:DefaultConnection` entry in `appsettings.json` or update the connection string in `Program.cs`.
- `EfUserRepository` implements `IUserRepository` using EF Core async patterns and `AsNoTracking()` for reads.
- JWT authentication is configured using `Microsoft.AspNetCore.Authentication.JwtBearer`. An `AuthController` provides a demo login endpoint at `POST /api/auth/login`. Use any username and the password `password` to receive a JWT token.

Get a token (example):

```powershell
curl -X POST -H "Content-Type: application/json" -d '{"username":"admin","password":"password"}' http://localhost:5000/api/auth/login
```

Use the returned token in requests:

```
Authorization: Bearer <token>
```

Security note: the demo login and symmetric key are for development/testing only. For production, use a secure credentials store, stronger secrets, rotate keys regularly, and validate audiences/issuers appropriately.

Pushing to GitHub

To push this project to your repository, run:

```powershell
git init
git add .
git commit -m "Initial UserManagementAPI scaffold with middleware and CRUD"
git remote add origin https://github.com/Tosinuel/UserManagementAPI-.git
git branch -M main
git push -u origin main
```

Be sure your local machine has appropriate access (SSH key or credentials) to push to the remote repo.

Migrations and seeding

- This project is configured to use EF Core migrations. To create and apply migrations locally you need the `dotnet-ef` tool installed. On your machine run:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

- The app will also call `Database.Migrate()` at startup to apply any pending migrations.

Seeding & secrets

- The admin user is seeded at startup if one does not exist. You should NOT leave a generated or default password in production. Provide an admin password via the environment variable `ADMIN_PASSWORD` before first start, or set `Admin:Password` in a secure `appsettings.Production.json` (prefer a secrets manager or environment variables instead):

```powershell
$env:ADMIN_PASSWORD = "<strong-password>"
dotnet run
```

- JWT secrets and other production secrets should be provided via environment variables or a secret manager. Example environment variables recognized:

	- `JWT__KEY` (the symmetric key used to sign tokens)
	- `JWT__ISSUER`
	- `JWT__AUDIENCE`

When a secure admin password is not provided, the app will generate a secure random password and print it to the console (development convenience only). Replace this with a proper secret provisioning flow for production.
