# Library Book Lending System

## 1. Project Overview

The Library Book Lending System is a web-based application for managing library book records, members, book reservations, borrowing, returns, and other library services.

It consists of an ASP.NET Core REST Web API backed by SQL Server (LocalDB) and an HTML/CSS/JavaScript front end served from the API's `wwwroot` folder. It uses JWT authentication and role-based authorization.

Developed as part of the requirements for Integrative Programming and Technologies I (IPT).

## 2. User Roles

### Librarian (administrator)
- Manage book records.
- Approve or reject book reservations.
- Record borrowings and returns, and monitor overdue borrowings.
- Manage seats, reading spaces, and their reservations.
- Monitor QCPL external reservations.
- View members, update membership expiry and account status, and view the summary report.

### Member
- Register and log in.
- Browse books and submit book reservations.
- View and cancel their own pending reservations.
- View their own borrowing records.
- Reserve seats and reading spaces.

Access to restricted operations is enforced by the API, not only hidden in the UI.

## 3. Features

- Registration and login with hashed passwords (`PasswordHasher`) and JWT bearer tokens.
- Role-based authorization (`401` for anonymous, `403` for the wrong role).
- Book CRUD (deactivation instead of hard delete).
- Book reservation workflow: Pending → Approved / Rejected / Cancelled / Expired.
- Borrowing workflow: Borrowed → Returned, with Overdue tracking.
- Seat and reading-space management and reservations (conflict checks enforced on the server).
- QCPL external reservation monitoring.
- Summary report endpoint shown on the Reports screen.

## 4. Technologies

C#, ASP.NET Core Web API (.NET 8), Entity Framework Core, SQL Server LocalDB, HTML, CSS, JavaScript (Fetch API), JWT Bearer, Swagger (Swashbuckle).

## 5. Prerequisites

- Visual Studio 2022 (or the .NET 8 SDK with a terminal)
- .NET 8 SDK
- SQL Server LocalDB (installed with Visual Studio's "ASP.NET and web development" workload)
- Git

## 6. Setup

### 6.1 Clone and restore

```bash
git clone <repository-url>
cd Library-Book-Lending
dotnet restore
```

### 6.2 Configure the JWT signing key (required)

The API will not start, and logins will fail, until `Jwt:Key` is set to a random string of **at least 32 characters**. Choose **one** of the two options below.

**Option A: User Secrets (recommended; nothing is committed)**

```bash
dotnet user-secrets set "Jwt:Key" "replace-this-with-your-own-random-32+-char-secret" --project "Library Book Lending.csproj"
```

In Visual Studio you can instead right-click the project → **Manage User Secrets** and add:

```json
{
  "Jwt": {
    "Key": "replace-this-with-your-own-random-32+-char-secret"
  }
}
```

**Option B: Edit `appsettings.json` locally (quickest for graders)**

Open `appsettings.json` and replace the blank value:

```json
"Jwt": {
  "Key": "replace-this-with-your-own-random-32+-char-secret",
  "Issuer": "Library_Book_Lending",
  "Audience": "LibraryBookLendingUsers",
  "ExpiryMinutes": "60"
}
```

> **Do not commit a real key.** If you use Option B, keep the change local (for example `git update-index --assume-unchanged appsettings.json`) or use a throwaway key that protects nothing.

### 6.3 Configure the database connection

The default connection string in `appsettings.json` targets LocalDB and works as-is:

```
Server=(localdb)\MSSQLLocalDB;Database=LibraryBookLendingDB;Trusted_Connection=True;MultipleActiveResultSets=true
```

Change it only if you use a different SQL Server instance.

### 6.4 Create the database

On startup the API applies all migrations and seeds test data automatically. To create the database manually beforehand:

```bash
dotnet ef database update
```

or in Visual Studio's Package Manager Console:

```powershell
Update-Database
```

To add a migration after model changes (coordinate with the other contributor first):

```powershell
Add-Migration MigrationName
Update-Database
```

## 7. Running the Application

**Visual Studio:** set the API project as the startup project and run the `http` or `https` profile.

**Terminal:**

```bash
dotnet run --launch-profile http
```

Always run with the `Development` environment (the launch profiles above already do), because Swagger is only enabled in Development.

| What | URL |
|---|---|
| Front end (HTML/JS client) | http://localhost:5241/ |
| Swagger UI | http://localhost:5241/swagger |
| Swagger UI (https profile) | https://localhost:7176/swagger |

The front end is served from `wwwroot/` by the same application, so there is no separate front-end server to start.

## 8. Test Accounts

All accounts are created automatically by the database seeder.

| Role      |          Email        |   Password  |
|    ---    |           ---         |     ---     |
| Librarian | librarian@library.com | Library123! |
| Librarian | admin@library.com     | Library123! |
| Member    | member@library.com    | Library123! |

Sample books, seats, and reading spaces are seeded as well. You can also register new accounts from the **Sign Up** tab; new accounts are always Members.

To test authorization quickly: log in as `member@library.com`, copy the token, and call a librarian endpoint such as `GET /api/Users` in Swagger. The API returns `403`. Calling it without a token returns `401`.

## 9. Frontend

Plain HTML, CSS, and JavaScript in `wwwroot/`. The client stores the JWT after login, sends it as `Authorization: Bearer <token>` on every request through the `apiRequest` helper in `wwwroot/js/api.js`, and shows role-appropriate menus. Server validation and business-rule errors are shown to the user as messages.

## 10. Project Structure

| Folder/File | Description |
|---|---|
| `Controllers/` | REST API controllers |
| `Data/` | Database context and seeder |
| `DTOs/` | Request and response objects |
| `Models/` | Entities and enums |
| `Migrations/` | EF Core migrations |
| `wwwroot/` | HTML, CSS, and JavaScript front end |
| `Program.cs` | Service configuration and middleware |

## 11. Authentication and Authorization

- `POST /api/Auth/register` creates a Member account; passwords are hashed with `PasswordHasher`.
- `POST /api/Auth/login` returns a JWT used for all protected calls.
- Endpoints are protected with `[Authorize]` and `[Authorize(Roles = "...")]`.
- Anonymous requests receive `401`; authenticated users with the wrong role receive `403`.

## 12. GitHub Collaboration

Work is done on feature branches and merged into `main` through pull requests. Per-member responsibilities and the actual contribution record are in `CONTRIBUTIONS.md`.

## 13. Contributors

- Kim Charlotte Santos
- Cyrill May Objero

## 14. Academic Purpose

This application was developed for academic purposes as part of the Integrative Programming and Technologies I course requirements.