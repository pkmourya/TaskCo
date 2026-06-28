using System.Net;
using System.Net.Http.Json;
using TaskCo.Api.Models.Dtos.Projects;
using TaskCo.Api.Models.Dtos.Tasks;
using TaskCo.Tests.Helpers;

namespace TaskCo.Tests.Integration;

public class TaskItemsControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TaskItemsControllerTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<int> SetupUserWithProjectAsync(string email)
    {
        var token = await AuthHelper.RegisterAndGetTokenAsync(_client, email);
        AuthHelper.SetBearerToken(_client, token);

        var resp = await _client.PostAsJsonAsync("/api/projects", new { name = "Test Project" });
        var project = await ApiHelpers.ReadDataAsync<ProjectResponse>(resp);
        return project!.Id;
    }

    private async Task<TaskResponse> CreateTaskAsync(int projectId, string title, string status = "Todo")
    {
        var resp = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks",
            new { title, status });
        return (await ApiHelpers.ReadDataAsync<TaskResponse>(resp))!;
    }

    // ── Unauthenticated ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/projects/1/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("unauthorized", code);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/projects/1/tasks", new { title = "T" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Not-found project ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NonExistentProject_Returns404()
    {
        var token = await AuthHelper.RegisterAndGetTokenAsync(_client, "user@example.com");
        AuthHelper.SetBearerToken(_client, token);

        var response = await _client.GetAsync("/api/projects/99999/tasks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task Create_NonExistentProject_Returns404()
    {
        var token = await AuthHelper.RegisterAndGetTokenAsync(_client, "user@example.com");
        AuthHelper.SetBearerToken(_client, token);

        var response = await _client.PostAsJsonAsync("/api/projects/99999/tasks", new { title = "T" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Authenticated: basic CRUD ────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Authenticated_ReturnsTasks()
    {
        var projectId = await SetupUserWithProjectAsync("user@example.com");
        await CreateTaskAsync(projectId, "Task A");
        await CreateTaskAsync(projectId, "Task B");

        var response = await _client.GetAsync($"/api/projects/{projectId}/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await ApiHelpers.ReadDataAsync<List<TaskResponse>>(response);
        Assert.Equal(2, tasks!.Count);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithDefaults()
    {
        var projectId = await SetupUserWithProjectAsync("user@example.com");

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks",
            new { title = "My Task", description = "A description" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await ApiHelpers.ReadDataAsync<TaskResponse>(response);
        Assert.NotNull(task);
        Assert.Equal("My Task", task.Title);
        Assert.Equal("A description", task.Description);
        Assert.Equal("Todo", task.Status.ToString());
        Assert.Equal("Medium", task.Priority.ToString());
    }

    [Fact]
    public async Task Create_WithExplicitStatusAndPriority_Returns201()
    {
        var projectId = await SetupUserWithProjectAsync("user@example.com");

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks",
            new { title = "Task", status = "InProgress", priority = "High" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await ApiHelpers.ReadDataAsync<TaskResponse>(response);
        Assert.Equal("InProgress", task!.Status.ToString());
        Assert.Equal("High", task.Priority.ToString());
    }

    [Fact]
    public async Task Create_EmptyTitle_Returns400WithValidationError()
    {
        var projectId = await SetupUserWithProjectAsync("user@example.com");

        var response = await _client.PostAsJsonAsync($"/api/projects/{projectId}/tasks",
            new { title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("validation_error", code);
    }

    [Fact]
    public async Task GetById_OwnedTask_Returns200()
    {
        var projectId = await SetupUserWithProjectAsync("user@example.com");
        var created = await CreateTaskAsync(projectId, "My Task");

        var response = await _client.GetAsync($"/api/projects/{projectId}/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await ApiHelpers.ReadDataAsync<TaskResponse>(response);
        Assert.Equal(created.Id, task!.Id);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var projectId = await SetupUserWithProjectAsync("user@example.com");

        var response = await _client.GetAsync($"/api/projects/{projectId}/tasks/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task Update_OwnedTask_Returns200WithUpdatedData()
    {
        var projectId = await SetupUserWithProjectAsync("user@example.com");
        var created = await CreateTaskAsync(projectId, "Old Title");

        var response = await _client.PutAsJsonAsync($"/api/projects/{projectId}/tasks/{created.Id}",
            new { title = "New Title", status = "Done", priority = "Low" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await ApiHelpers.ReadDataAsync<TaskResponse>(response);
        Assert.Equal("New Title", task!.Title);
        Assert.Equal("Done", task.Status.ToString());
    }

    [Fact]
    public async Task Delete_OwnedTask_Returns204()
    {
        var projectId = await SetupUserWithProjectAsync("user@example.com");
        var created = await CreateTaskAsync(projectId, "To Delete");

        var response = await _client.DeleteAsync($"/api/projects/{projectId}/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/projects/{projectId}/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    // ── Cross-user access ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_AnotherUsersProject_Returns404()
    {
        // User 1 creates a project with a task
        var token1 = await AuthHelper.RegisterAndGetTokenAsync(_client, "user1@example.com");
        AuthHelper.SetBearerToken(_client, token1);
        var resp = await _client.PostAsJsonAsync("/api/projects", new { name = "U1 Project" });
        var project = await ApiHelpers.ReadDataAsync<ProjectResponse>(resp);

        // User 2 tries to list tasks for User 1's project
        var client2 = _factory.CreateClient();
        var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
        AuthHelper.SetBearerToken(client2, token2);

        var response = await client2.GetAsync($"/api/projects/{project!.Id}/tasks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task GetById_AnotherUsersTask_Returns404()
    {
        // User 1 creates a project and task
        var projectId = await SetupUserWithProjectAsync("user1@example.com");
        var task = await CreateTaskAsync(projectId, "U1 Task");

        // User 2 tries to get it
        var client2 = _factory.CreateClient();
        var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
        AuthHelper.SetBearerToken(client2, token2);

        var response = await client2.GetAsync($"/api/projects/{projectId}/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task Create_InAnotherUsersProject_Returns404()
    {
        // User 1 creates a project
        var projectId = await SetupUserWithProjectAsync("user1@example.com");

        // User 2 tries to create a task in it
        var client2 = _factory.CreateClient();
        var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
        AuthHelper.SetBearerToken(client2, token2);

        var response = await client2.PostAsJsonAsync($"/api/projects/{projectId}/tasks",
            new { title = "Injected Task" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task Update_AnotherUsersTask_Returns404()
    {
        // User 1 creates a project and task
        var projectId = await SetupUserWithProjectAsync("user1@example.com");
        var task = await CreateTaskAsync(projectId, "U1 Task");

        // User 2 tries to update it
        var client2 = _factory.CreateClient();
        var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
        AuthHelper.SetBearerToken(client2, token2);

        var response = await client2.PutAsJsonAsync($"/api/projects/{projectId}/tasks/{task.Id}",
            new { title = "Hijacked", status = "Done", priority = "Low" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task Delete_AnotherUsersTask_Returns404()
    {
        // User 1 creates a project and task
        var projectId = await SetupUserWithProjectAsync("user1@example.com");
        var task = await CreateTaskAsync(projectId, "U1 Task");

        // User 2 tries to delete it
        var client2 = _factory.CreateClient();
        var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
        AuthHelper.SetBearerToken(client2, token2);

        var response = await client2.DeleteAsync($"/api/projects/{projectId}/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }
}
