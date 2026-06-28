using System.Net;
using System.Net.Http.Json;
using TaskCo.Api.Models.Dtos.Projects;
using TaskCo.Tests.Helpers;

namespace TaskCo.Tests.Integration;

public class ProjectsControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectsControllerTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> LoginAsAsync(string email)
    {
        var token = await AuthHelper.RegisterAndGetTokenAsync(_client, email);
        return token;
    }

    private async Task<ProjectResponse> CreateProjectAsync(string name, string? description = null)
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name, description });
        return (await ApiHelpers.ReadDataAsync<ProjectResponse>(response))!;
    }

    // ── Unauthenticated ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("unauthorized", code);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name = "Test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Authenticated: basic CRUD ────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Authenticated_ReturnsOwnedProjects()
    {
        var token = await LoginAsAsync("user@example.com");
        AuthHelper.SetBearerToken(_client, token);

        await CreateProjectAsync("Project A");
        await CreateProjectAsync("Project B");

        var response = await _client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await ApiHelpers.ReadDataAsync<List<ProjectResponse>>(response);
        Assert.NotNull(projects);
        Assert.Equal(2, projects.Count);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithProject()
    {
        var token = await LoginAsAsync("user@example.com");
        AuthHelper.SetBearerToken(_client, token);

        var response = await _client.PostAsJsonAsync("/api/projects",
            new { name = "My Project", description = "A description" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = await ApiHelpers.ReadDataAsync<ProjectResponse>(response);
        Assert.NotNull(project);
        Assert.Equal("My Project", project.Name);
        Assert.Equal("A description", project.Description);
        Assert.True(project.Id > 0);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400WithValidationError()
    {
        var token = await LoginAsAsync("user@example.com");
        AuthHelper.SetBearerToken(_client, token);

        var response = await _client.PostAsJsonAsync("/api/projects", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("validation_error", code);
    }

    [Fact]
    public async Task GetById_OwnedProject_Returns200()
    {
        var token = await LoginAsAsync("user@example.com");
        AuthHelper.SetBearerToken(_client, token);

        var created = await CreateProjectAsync("My Project");

        var response = await _client.GetAsync($"/api/projects/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await ApiHelpers.ReadDataAsync<ProjectResponse>(response);
        Assert.Equal(created.Id, project!.Id);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var token = await LoginAsAsync("user@example.com");
        AuthHelper.SetBearerToken(_client, token);

        var response = await _client.GetAsync("/api/projects/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task Update_OwnedProject_Returns200WithUpdatedData()
    {
        var token = await LoginAsAsync("user@example.com");
        AuthHelper.SetBearerToken(_client, token);

        var created = await CreateProjectAsync("Original Name");

        var response = await _client.PutAsJsonAsync($"/api/projects/{created.Id}",
            new { name = "Updated Name", description = "New desc" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await ApiHelpers.ReadDataAsync<ProjectResponse>(response);
        Assert.Equal("Updated Name", project!.Name);
        Assert.Equal("New desc", project.Description);
    }

    [Fact]
    public async Task Delete_OwnedProject_Returns204()
    {
        var token = await LoginAsAsync("user@example.com");
        AuthHelper.SetBearerToken(_client, token);

        var created = await CreateProjectAsync("To Delete");

        var response = await _client.DeleteAsync($"/api/projects/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify gone
        var getResponse = await _client.GetAsync($"/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    // ── Cross-user access ────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_AnotherUsersProject_Returns404()
    {
        // User 1 creates a project
        var token1 = await AuthHelper.RegisterAndGetTokenAsync(_client, "user1@example.com");
        AuthHelper.SetBearerToken(_client, token1);
        var project = await CreateProjectAsync("User1 Project");

        // User 2 tries to read it — must get 404, not 403
        var client2 = _factory.CreateClient();
        var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
        AuthHelper.SetBearerToken(client2, token2);

        var response = await client2.GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task Update_AnotherUsersProject_Returns404()
    {
        // User 1 creates a project
        var token1 = await AuthHelper.RegisterAndGetTokenAsync(_client, "user1@example.com");
        AuthHelper.SetBearerToken(_client, token1);
        var project = await CreateProjectAsync("User1 Project");

        // User 2 tries to update it
        var client2 = _factory.CreateClient();
        var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
        AuthHelper.SetBearerToken(client2, token2);

        var response = await client2.PutAsJsonAsync($"/api/projects/{project.Id}",
            new { name = "Hijacked" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task Delete_AnotherUsersProject_Returns404()
    {
        // User 1 creates a project
        var token1 = await AuthHelper.RegisterAndGetTokenAsync(_client, "user1@example.com");
        AuthHelper.SetBearerToken(_client, token1);
        var project = await CreateProjectAsync("User1 Project");

        // User 2 tries to delete it
        var client2 = _factory.CreateClient();
        var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
        AuthHelper.SetBearerToken(client2, token2);

        var response = await client2.DeleteAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("not_found", code);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyOwnedProjects_NotOtherUsers()
    {
        // User 1 creates two projects
        var token1 = await AuthHelper.RegisterAndGetTokenAsync(_client, "user1@example.com");
        AuthHelper.SetBearerToken(_client, token1);
        await CreateProjectAsync("User1 Project A");
        await CreateProjectAsync("User1 Project B");

        // User 2 creates one project and checks their list
        var client2 = _factory.CreateClient();
        var token2 = await AuthHelper.RegisterAndGetTokenAsync(client2, "user2@example.com");
        AuthHelper.SetBearerToken(client2, token2);
        await client2.PostAsJsonAsync("/api/projects", new { name = "User2 Project" });

        var response = await client2.GetAsync("/api/projects");
        var projects = await ApiHelpers.ReadDataAsync<List<ProjectResponse>>(response);

        Assert.Single(projects!);
        Assert.Equal("User2 Project", projects![0].Name);
    }
}
