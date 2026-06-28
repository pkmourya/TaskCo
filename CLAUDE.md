# TaskCo — CLAUDE.md

## What this is

TaskCo is a multi-user task manager. It is **complete**. Do not add features beyond what is
documented here. If something seems needed but isn't listed, ask before building it.

---

## Runtime

The machine has **.NET 9 only** (no .NET 8 runtime installed). Every package is pinned to the
9.x series even though the original spec said .NET 8:

- `TargetFramework`: `net9.0`
- EF Core: `9.0.6` (Microsoft.EntityFrameworkCore.SqlServer, .Design, .Tools, .InMemory)
- JwtBearer: `9.0.6` (Microsoft.AspNetCore.Authentication.JwtBearer)
- FluentValidation: `11.10.0` (FluentValidation.DependencyInjectionExtensions)

The local `dotnet-ef` tool is version `9.0.6`, pinned in `.config/dotnet-tools.json`. Always
run migrations with `dotnet tool run dotnet-ef` (not the global `dotnet ef` which is 10.x).

---

## Solution layout

```
TaskCo.sln
├── src/
│   ├── TaskCo.Api/          ← API + Razor Pages (hybrid host)
│   └── TaskCo.Web/          ← standalone Razor Pages (HTTP-client-based, secondary)
└── tests/
    └── TaskCo.Tests/        ← xUnit tests
```

> **TaskCo.Api is the primary host.** It serves both `api/...` (REST controllers) and `/` (Razor
> Pages) in a single process. `TaskCo.Web` is a standalone alternative that calls the API over
> HTTP; it still builds and its tests still pass, but it is not the main frontend.

---

## TaskCo.Api — complete file tree

```
Controllers/
  AuthController.cs
  ProjectsController.cs
  TaskItemsController.cs
Data/
  AppDbContext.cs
  Configurations/
    ProjectConfiguration.cs
    TaskItemConfiguration.cs
    UserConfiguration.cs
  Migrations/
    20260627161612_InitialCreate.cs
    20260627161612_InitialCreate.Designer.cs
    AppDbContextModelSnapshot.cs
Exceptions/
  AppException.cs          ← base: int StatusCode, string Code
  ConflictException.cs     ← 409 "conflict"
  NotFoundException.cs     ← 404 "not_found"
  UnauthorizedException.cs ← 401 "unauthorized"
Middleware/
  ExceptionMiddleware.cs
Models/
  Common/
    ApiResponse.cs         ← static Success<T> / Failure helpers
  Dtos/
    Auth/
      AuthResponse.cs      ← Token, UserId, Email
      LoginRequest.cs
      RegisterRequest.cs
    Projects/
      CreateProjectRequest.cs
      ProjectResponse.cs
      UpdateProjectRequest.cs
    Tasks/
      CreateTaskRequest.cs
      TaskResponse.cs
      UpdateTaskRequest.cs
  Entities/
    User.cs
    Project.cs
    TaskItem.cs            ← enums: TaskItemStatus, TaskItemPriority
Pages/                     ← Razor Pages (cookie auth, direct service DI)
  _ViewImports.cshtml
  _ViewStart.cshtml
  Shared/
    _Layout.cshtml
  Login.cshtml / .cs
  Register.cshtml / .cs
  Logout.cshtml / .cs
  Index.cshtml / .cs       ← dashboard: project list + inline create
  Projects/
    Details.cshtml / .cs   ← task list + filter + inline create
Services/
  Interfaces/
    IAuthService.cs
    IProjectService.cs
    ITaskService.cs
    ITokenService.cs
  AuthService.cs
  JwtSettings.cs
  ProjectService.cs
  TaskService.cs
  TokenService.cs
Validators/
  Auth/
    LoginRequestValidator.cs
    RegisterRequestValidator.cs
  Projects/
    CreateProjectRequestValidator.cs
    UpdateProjectRequestValidator.cs
  Tasks/
    CreateTaskRequestValidator.cs
    UpdateTaskRequestValidator.cs
wwwroot/
  lib/bootstrap/           ← Bootstrap 5 (local, copied from TaskCo.Web)
  lib/jquery/
  css/
  js/
Program.cs
appsettings.json
```

---

## TaskCo.Tests — file tree

```
Helpers/
  ApiHelpers.cs            ← ReadDataAsync<T>, ReadErrorAsync
  AuthHelper.cs            ← RegisterAndGetTokenAsync, SetBearerToken
  TestWebApplicationFactory.cs
Integration/
  AuthControllerTests.cs
  ProjectsControllerTests.cs
  TaskItemsControllerTests.cs
Unit/
  AuthServiceTests.cs
  ProjectServiceTests.cs
  RegisterRequestValidatorTests.cs
  TaskServiceTests.cs
```

73 tests, all passing. Run with:

```bash
dotnet test TaskCo.sln
```

---

## Domain model

```
User  1──<N  Project  1──<N  TaskItem
              OwnerId              ProjectId
```

- `Project.OwnerId` → `User.Id`
- Tasks inherit ownership through their project (`t.Project.OwnerId == userId`)
- **Never** read the current user ID from the request body or query string. Always from the JWT
  claim `ClaimTypes.NameIdentifier` server-side.
- **Return `not_found` (404), not 403**, when a resource is missing or owned by someone else.

### Entities

```csharp
public class User    { int Id; string Email; string PasswordHash; DateTime CreatedAt; ICollection<Project> Projects; }
public class Project { int Id; string Name; string? Description; int OwnerId; DateTime CreatedAt; DateTime UpdatedAt; User Owner; ICollection<TaskItem> TaskItems; }
public class TaskItem{ int Id; string Title; string? Description; TaskItemStatus Status; TaskItemPriority Priority; DateTime? DueDate; int ProjectId; DateTime CreatedAt; DateTime UpdatedAt; Project Project; }

public enum TaskItemStatus   { Todo, InProgress, Done }     // never "TaskStatus" — conflicts with System.Threading.Tasks
public enum TaskItemPriority { Low, Medium, High }          // never "TaskPriority"
```

Enum columns are stored as `string` via `HasConversion<string>()` in `TaskItemConfiguration`.
This means renaming the enums does **not** require a new migration.

---

## Authentication: two schemes in one host

```
Cookie ("Cookies") — default scheme — used by Razor Pages
Bearer (JWT)       — named scheme   — used by API controllers
```

```csharp
// Program.cs (simplified)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => { options.LoginPath = "/Login"; ... })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => { ... });
```

- API controllers: `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`
- Razor Pages: `[Authorize]` (uses cookie default)
- After login/register, page models call `HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal)` with claims `NameIdentifier` (userId) + `Email`.

### JWT settings (appsettings.json)

```json
"Jwt": {
  "Key":              "your-super-secret-key-must-be-at-least-32-characters-long!",
  "Issuer":           "TaskCo",
  "Audience":         "TaskCo",
  "ExpiresInMinutes": 60
}
```

**Critical for tests**: `TestWebApplicationFactory` does NOT override the JWT key. The token is
generated and validated with the same key from `appsettings.json`. Any attempt to override JWT
settings via `ConfigureAppConfiguration` in the factory will cause a key mismatch and 401s because
`WebApplicationBuilder.Configuration` is captured before the factory's callbacks run.

### JWT claims

```csharp
// TokenService
new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
new Claim(ClaimTypes.Email, user.Email)
new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
```

### AuthResponse

```csharp
public class AuthResponse { string Token; int UserId; string Email; }
```

All three fields are returned from `IAuthService.LoginAsync` and `.RegisterAsync` so that page
models can issue cookie claims without parsing the JWT.

---

## Response envelope (every response)

```json
// success
{ "data": <payload> }

// failure
{ "error": { "code": "snake_case_code", "message": "...", "details?": [...] } }
```

Error codes: `validation_error`, `unauthorized`, `not_found`, `conflict`, `internal_error`.

`ApiResponse.cs`:
```csharp
public static object Success<T>(T data) => new { data };
public static object Failure(string code, string message, object? details = null)
    => new { error = new { code, message, details } };
```

`details` is omitted from JSON when null because the serializer has
`DefaultIgnoreCondition = WhenWritingNull`.

`ExceptionMiddleware` catches `AppException` subclasses and unknown exceptions, writing the envelope
directly to the response. It runs before everything else so even 401s from JwtBearer go through
the envelope (via `JwtBearerEvents.OnChallenge`).

---

## JSON serialization

```csharp
options.JsonSerializerOptions.PropertyNamingPolicy           = JsonNamingPolicy.CamelCase;
options.JsonSerializerOptions.DefaultIgnoreCondition         = JsonIgnoreCondition.WhenWritingNull;
options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
```

Enums serialize as `"Todo"`, `"InProgress"`, `"Done"`, `"Low"`, `"Medium"`, `"High"` (strings,
not integers). Tests and frontend must send/receive these string values.

Model-state auto-validation is suppressed:
```csharp
builder.Services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);
```
Validation is done manually in controllers via `IValidator<T>.ValidateAsync`.

---

## Service interfaces (all in `Services/Interfaces/`)

```csharp
IAuthService
  Task<AuthResponse> RegisterAsync(RegisterRequest request)
  Task<AuthResponse> LoginAsync(LoginRequest request)

IProjectService
  Task<IEnumerable<ProjectResponse>> GetAllAsync(int userId)
  Task<ProjectResponse>              GetByIdAsync(int id, int userId)
  Task<ProjectResponse>              CreateAsync(CreateProjectRequest request, int userId)
  Task<ProjectResponse>              UpdateAsync(int id, UpdateProjectRequest request, int userId)
  Task                               DeleteAsync(int id, int userId)

ITaskService
  Task<IEnumerable<TaskResponse>> GetAllAsync(int projectId, int userId)
  Task<TaskResponse>              GetByIdAsync(int projectId, int id, int userId)
  Task<TaskResponse>              CreateAsync(int projectId, CreateTaskRequest request, int userId)
  Task<TaskResponse>              UpdateAsync(int projectId, int id, UpdateTaskRequest request, int userId)
  Task                            DeleteAsync(int projectId, int id, int userId)
```

### Ownership queries

```csharp
// ProjectService — every query
.Where(p => p.OwnerId == userId)

// TaskService — GetAll/Create checks project ownership first
await _db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId)
    || throw new NotFoundException(...)

// TaskService — GetById/Update/Delete uses join in one query
await _db.TaskItems.SingleOrDefaultAsync(
    t => t.Id == id && t.ProjectId == projectId && t.Project.OwnerId == userId)
```

### CreateTaskRequest defaults

`Status` and `Priority` are nullable in `CreateTaskRequest`. `TaskService.CreateAsync` defaults:
- `Status ?? TaskItemStatus.Todo`
- `Priority ?? TaskItemPriority.Medium`

`UpdateTaskRequest` has non-nullable `Status` and `Priority` (both required on update).

---

## Controller pattern

```csharp
[ApiController]
[Route("api/projects")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProjectsController : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Failure("validation_error", "Validation failed",
                validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })));

        var project = await _projectService.CreateAsync(request, CurrentUserId);
        return StatusCode(201, ApiResponse.Success(project));
    }
}
```

Tasks controller route: `[Route("api/projects/{projectId:int}/tasks")]`

---

## Middleware pipeline order (Program.cs)

```csharp
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();        // serves wwwroot for Razor Pages
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
```

`public partial class Program { }` at the bottom of `Program.cs` (required by
`WebApplicationFactory<Program>` in tests).

---

## EF Core / Migrations

```bash
# Always use the local tool (global dotnet-ef is 10.x and needs .NET 8)
dotnet tool run dotnet-ef migrations add <Name> --project src/TaskCo.Api
dotnet tool run dotnet-ef database update --project src/TaskCo.Api
```

Connection string (localdb, dev only):
```
Server=(localdb)\mssqllocaldb;Database=TaskCo;Trusted_Connection=True;MultipleActiveResultSets=true
```

---

## Integration tests — critical rules

### TestWebApplicationFactory

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DbName { get; } = Guid.NewGuid().ToString();  // fresh DB per test class

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove SQL Server provider. Must remove ALL three or EF Core 9 complains about
            // multiple providers (IDbContextOptionsConfiguration<T> is internal — match by
            // generic type argument).
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GenericTypeArguments.Any(a => a == typeof(AppDbContext))))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(DbName));
        });
        // !! Do NOT override JWT/Jwt:Key here — WebApplicationBuilder.Configuration is already
        // captured at startup; ConfigureAppConfiguration on IWebHostBuilder only affects
        // IWebHostBuilder's layer and does NOT reach builder.Configuration. Key mismatch → 401.
    }
}
```

### Cross-user test pattern

```csharp
// user1 creates resource
var token1 = await AuthHelper.RegisterAndGetTokenAsync(_client, "user1@example.com");
AuthHelper.SetBearerToken(_client, token1);
var resp = await _client.PostAsJsonAsync("/api/projects", new { name = "P1" });
var project = await ApiHelpers.ReadDataAsync<ProjectResponse>(resp);

// user2 attempts to access it (same factory → same InMemory DB)
var client2 = _factory.CreateClient();
var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
AuthHelper.SetBearerToken(client2, token2);
var response = await client2.GetAsync($"/api/projects/{project!.Id}");
Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
```

### ApiHelpers deserializer

Must include `JsonStringEnumConverter` or `TaskItemStatus`/`TaskItemPriority` deserialization
fails in tests:

```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};
```

---

## Razor Pages (in TaskCo.Api/Pages/)

Pages use **cookie auth** and call services directly via DI — no HTTP calls to the API.

| Page | Route | Auth |
|---|---|---|
| `Login.cshtml` | `/Login` | anonymous |
| `Register.cshtml` | `/Register` | anonymous |
| `Logout.cshtml` | `/Logout` | POST clears cookie |
| `Index.cshtml` | `/` | `[Authorize]` — dashboard + inline create project |
| `Projects/Details.cshtml` | `/Projects/Details?projectId=N` | `[Authorize]` — tasks + filter + inline create task |

### Page model user ID

```csharp
private int CurrentUserId =>
    int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

### Task filter on Details page

GET `/Projects/Details?projectId=1&filter=InProgress`

```csharp
if (!string.IsNullOrEmpty(Filter) && Enum.TryParse<TaskItemStatus>(Filter, out var filterEnum))
    Tasks = all.Where(t => t.Status == filterEnum).ToList();
else
    Tasks = all;
```

Valid filter values: `Todo`, `InProgress`, `Done` (exact enum name, case-sensitive).

### Named POST handlers on Details page

- `asp-page-handler="CreateTask"` → `OnPostCreateTaskAsync()`
- `asp-page-handler="DeleteTask"` → `OnPostDeleteTaskAsync(int projectId, int taskId)`

### Bootstrap

Bootstrap 5 and jQuery are in `TaskCo.Api/wwwroot/lib/` (local files, not CDN).
Referenced as `~/lib/bootstrap/dist/css/bootstrap.min.css` etc.

Badge classes used:
- Status Todo → `badge bg-secondary`
- Status InProgress → `badge bg-primary`
- Status Done → `badge bg-success`
- Priority High → `badge bg-danger`
- Priority Medium → `badge bg-warning text-dark`
- Priority Low → `badge bg-success`

---

## Out of scope — never add

Roles/admin, project sharing, password reset, email verification, refresh tokens, OAuth,
SignalR, notifications, attachments, comments, tags, subtasks, pagination/search beyond basic
list, soft deletes, audit logs, Docker/CI/CD, API versioning, SPA frontend.

---

## Security

### Login — constant-time verification (timing attack prevention)

`AuthService.LoginAsync` always calls `VerifyHashedPassword`, even when the email is not found.
Before this fix the `||` short-circuit meant a bad email returned in < 1 ms while a bad password
took ~100 ms (bcrypt), leaking whether an email was registered via response timing.

```csharp
// _dummyHash is a valid bcrypt string computed once at class init
private static readonly string _dummyHash =
    new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString());

var hashToVerify = user?.PasswordHash ?? _dummyHash;
var verified = _passwordHasher.VerifyHashedPassword(
    user ?? new User(), hashToVerify, request.Password);

if (user is null || verified == PasswordVerificationResult.Failed)
    throw new UnauthorizedException("Invalid credentials");
```

**Do not revert** this to a short-circuiting `if (user is null || Verify(...))` pattern.
The `_dummyHash` must be a genuine bcrypt hash (not a random string) so the full work factor runs.

### Registration — email existence disclosure (accepted trade-off)

`RegisterAsync` throws `ConflictException("Email is already registered")` when a duplicate email
is submitted, and the API returns HTTP 409. This intentionally tells the submitter the email is in
use, which is standard UX for apps without email verification (out of scope). The trade-off is
accepted: the alternative (silently succeeding and sending a "check your email" message) requires
email verification, which is not in scope.

### Error responses — no internal detail leakage

`ExceptionMiddleware` catches all exceptions:
- `AppException` subclasses → uses `ex.Code` and `ex.Message` (all manually controlled strings)
- Unknown exceptions → logs server-side via `ILogger`, returns only `"An unexpected error occurred"`

No stack traces, file paths, or database error strings ever reach the response body. In production
(Azure), set `ASPNETCORE_ENVIRONMENT=Production` to ensure the developer exception page is
disabled.

### Input validation

Every mutating endpoint runs `IValidator<T>.ValidateAsync()` before calling services.
`ApiBehaviorOptions.SuppressModelStateInvalidFilter = true` disables the automatic model-state
response so validation is always controlled by the controller, never by the framework silently.
All database queries use EF Core LINQ — no raw SQL, no string interpolation into queries.

### Dependency health (last checked 2026-06-28)

```
dotnet list package --vulnerable
→ The given project has no vulnerable packages.   (all three projects)
```

Re-run this before any production deployment or when adding packages.

### Packages in use (verify any additions on nuget.org before adding)

| Package | Version | Project |
|---|---|---|
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.6 | Api |
| Microsoft.EntityFrameworkCore.Design | 9.0.6 | Api |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.6 | Tests |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.6 | Api |
| FluentValidation.DependencyInjectionExtensions | 11.10.0 | Api |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.6 | Tests |

---

## Known pitfalls

| Pitfall | Fix |
|---|---|
| `TaskStatus` enum name conflicts with `System.Threading.Tasks.TaskStatus` (implicit usings in Web SDK) | Always name the enum `TaskItemStatus` and `TaskItemPriority` |
| EF Core 9 registers internal `IDbContextOptionsConfiguration<AppDbContext>` alongside `DbContextOptions<AppDbContext>` | Remove by checking `d.ServiceType.GenericTypeArguments.Any(a => a == typeof(AppDbContext))` |
| `ConfigureAppConfiguration` in `WebApplicationFactory.ConfigureWebHost` does not update `builder.Configuration` already captured in `Program.cs` top-level statements | Do not override JWT key in the test factory; let both generation and validation use `appsettings.json` |
| Global `dotnet-ef` tool (10.0.9) requires .NET 8 runtime | Use `dotnet tool run dotnet-ef` with the local 9.0.6 manifest |
| `ApiHelpers.ReadDataAsync<T>` fails on `TaskItemStatus` without `JsonStringEnumConverter` | Always include `new JsonStringEnumConverter()` in test `JsonSerializerOptions` |
| Cookie default auth breaks API controllers that use `[Authorize]` alone | All API controllers must specify `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` |

---

## Running the project

```bash
# API (also serves Razor Pages at http://localhost:5212)
dotnet run --project src/TaskCo.Api

# All tests
dotnet test TaskCo.sln

# Single migration
dotnet tool run dotnet-ef migrations add <Name> --project src/TaskCo.Api
dotnet tool run dotnet-ef database update --project src/TaskCo.Api
```

Ports (from launchSettings.json):
- API HTTP:  `http://localhost:5212`
- API HTTPS: `https://localhost:7079`
- Web HTTP:  `http://localhost:5030`
- Web HTTPS: `https://localhost:7274`
