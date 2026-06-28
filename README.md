# TaskCo

A multi-user task management web application. Users register, log in, create Projects, and manage Tasks within each project. Every user sees and controls only their own data.

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9, ASP.NET Core |
| API | ASP.NET Core Web API (controllers) + Razor Pages (UI) |
| ORM | Entity Framework Core 9 (code-first, migrations) |
| Database | Azure SQL (SQL Server) |
| Auth | ASP.NET Core Identity `PasswordHasher`, JWT Bearer, Cookie auth |
| Validation | FluentValidation |
| Tests | xUnit, `WebApplicationFactory` (integration), in-memory EF Core |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- Visual Studio 2022 (or VS Code with the C# Dev Kit extension)
- An Azure account with an Azure SQL database provisioned
- EF Core CLI tools:

```bash
dotnet tool install --global dotnet-ef
```

> If the global tool version conflicts with .NET 9, use the local manifest tool instead:
> `dotnet tool run dotnet-ef`

## Local Setup

### 1. Clone and restore

```bash
git clone <repo-url>
cd TaskCo
dotnet restore
```

### 2. Configure secrets

Never put credentials in `appsettings.json`. Use .NET user-secrets:

```bash
# Connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=tcp:<your-server>.database.windows.net,1433;Database=<db>;User ID=<user>;Password=<pass>;Encrypt=true;TrustServerCertificate=False;MultipleActiveResultSets=true" \
  --project src/TaskCo.Api

# JWT signing key — use a random 32-byte value, never the placeholder
dotnet user-secrets set "Jwt:Key" "<openssl rand -base64 32 output>" \
  --project src/TaskCo.Api
```

### 3. Apply migrations

```bash
dotnet tool run dotnet-ef database update --project src/TaskCo.Api
```

### 4. Run

```bash
dotnet watch run --project src/TaskCo.Api
```

The app is available at `http://localhost:5212` (HTTP) and `https://localhost:7079` (HTTPS).  
The Razor Pages UI is at `/` and the REST API is at `/api/...`.

### 5. Run tests

```bash
dotnet test TaskCo.sln
```

Tests use an in-memory database — no external database connection required.

## Azure Deployment

### 1. Configure Application Settings in the Azure portal

In your App Service → **Settings → Environment variables**, add:

| Name | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | Your Azure SQL connection string |
| `Jwt__Key` | A cryptographically random 32-byte base64 string |
| `Jwt__Issuer` | `TaskCo` |
| `Jwt__Audience` | `TaskCo` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

> Use double underscores (`__`) as the hierarchy separator in Azure App Service.

### 2. Publish

**Option A — Visual Studio 2022**

Right-click `TaskCo.Api` → Publish → Azure → App Service → follow the wizard.

**Option B — VS Code**

Install the [Azure App Service extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azureappservice), then right-click the project and choose **Deploy to Web App**.

**Option C — Azure CLI**

```bash
dotnet publish src/TaskCo.Api -c Release -o ./publish

az webapp deploy \
  --resource-group <rg> \
  --name <app-service-name> \
  --src-path ./publish \
  --type zip
```

### 3. Apply migrations against the production database

```bash
dotnet tool run dotnet-ef database update \
  --project src/TaskCo.Api \
  --connection "<production connection string>"
```

## API Reference

All responses use a consistent envelope:

```json
// success
{ "data": <payload> }

// failure
{ "error": { "code": "snake_case_code", "message": "...", "details": [...] } }
```

Error codes: `validation_error`, `unauthorized`, `not_found`, `conflict`, `internal_error`.

Protected endpoints require `Authorization: Bearer <token>` obtained from login or register.

---

### Auth

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | None | Register a new user account |
| `POST` | `/api/auth/login` | None | Log in and receive a JWT |

**POST /api/auth/register**

```json
// request
{ "email": "user@example.com", "password": "password123" }

// 201 response
{ "data": { "token": "<jwt>", "userId": 1, "email": "user@example.com" } }
```

**POST /api/auth/login**

```json
// request
{ "email": "user@example.com", "password": "password123" }

// 200 response
{ "data": { "token": "<jwt>", "userId": 1, "email": "user@example.com" } }
```

---

### Projects

All project endpoints are scoped to the authenticated user. A project owned by another user returns `404 not_found`.

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/projects` | Bearer | List all projects for the current user |
| `GET` | `/api/projects/{id}` | Bearer | Get a single project |
| `POST` | `/api/projects` | Bearer | Create a project |
| `PUT` | `/api/projects/{id}` | Bearer | Update a project |
| `DELETE` | `/api/projects/{id}` | Bearer | Delete a project and all its tasks |

**POST /api/projects**

```json
// request
{ "name": "My Project", "description": "Optional description" }

// 201 response
{ "data": { "id": 1, "name": "My Project", "description": "Optional description", "createdAt": "...", "updatedAt": "..." } }
```

**PUT /api/projects/{id}**

```json
// request — all fields required
{ "name": "Renamed Project", "description": "Updated description" }
```

---

### Tasks

Tasks are nested under a project. All task endpoints verify that the parent project is owned by the authenticated user.

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/projects/{projectId}/tasks` | Bearer | List all tasks in a project |
| `GET` | `/api/projects/{projectId}/tasks/{id}` | Bearer | Get a single task |
| `POST` | `/api/projects/{projectId}/tasks` | Bearer | Create a task |
| `PUT` | `/api/projects/{projectId}/tasks/{id}` | Bearer | Update a task |
| `DELETE` | `/api/projects/{projectId}/tasks/{id}` | Bearer | Delete a task |

**POST /api/projects/{projectId}/tasks**

```json
// request — status and priority are optional, defaulting to "Todo" and "Medium"
{
  "title": "Fix the login bug",
  "description": "Optional",
  "status": "Todo",
  "priority": "High",
  "dueDate": "2026-07-15T00:00:00Z"
}

// 201 response
{
  "data": {
    "id": 1,
    "title": "Fix the login bug",
    "description": "Optional",
    "status": "Todo",
    "priority": "High",
    "dueDate": "2026-07-15T00:00:00Z",
    "projectId": 1,
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```

**PUT /api/projects/{projectId}/tasks/{id}**

```json
// request — all fields required on update
{
  "title": "Fix the login bug",
  "description": "Updated notes",
  "status": "InProgress",
  "priority": "High",
  "dueDate": "2026-07-20T00:00:00Z"
}
```

**Valid enum values**

| Field | Values |
|---|---|
| `status` | `Todo`, `InProgress`, `Done` |
| `priority` | `Low`, `Medium`, `High` |

## Project Structure

```
TaskCo.sln
├── src/
│   ├── TaskCo.Api/      — REST API + Razor Pages (primary host)
│   └── TaskCo.Web/      — standalone Razor Pages calling the API over HTTP
└── tests/
    └── TaskCo.Tests/    — xUnit unit and integration tests (73 tests)
```
