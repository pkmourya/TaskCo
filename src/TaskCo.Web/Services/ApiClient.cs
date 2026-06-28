using System.Text.Json;
using TaskCo.Web.Models;

namespace TaskCo.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public string? LastError { get; private set; }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    private async Task<T?> ReadDataAsync<T>(HttpResponseMessage response)
    {
        LastError = null;
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("error", out var err) &&
                    err.TryGetProperty("message", out var msg))
                    LastError = msg.GetString();
            }
            catch { }
            LastError ??= $"Request failed ({(int)response.StatusCode})";
            return default;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("data", out var data))
                return data.Deserialize<T>(JsonOpts);
        }
        catch (Exception ex)
        {
            LastError = $"Failed to parse response: {ex.Message}";
        }
        return default;
    }

    // ── Auth ──────────────────────────────────────────────────────────────────

    public async Task<string?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
        var result = await ReadDataAsync<AuthResponse>(response);
        return result?.Token;
    }

    public async Task<string?> RegisterAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", new { email, password });
        var result = await ReadDataAsync<AuthResponse>(response);
        return result?.Token;
    }

    // ── Projects ──────────────────────────────────────────────────────────────

    public async Task<List<ProjectModel>?> GetProjectsAsync()
    {
        var response = await _http.GetAsync("api/projects");
        return await ReadDataAsync<List<ProjectModel>>(response);
    }

    public async Task<ProjectModel?> GetProjectAsync(int id)
    {
        var response = await _http.GetAsync($"api/projects/{id}");
        return await ReadDataAsync<ProjectModel>(response);
    }

    public async Task<ProjectModel?> CreateProjectAsync(string name, string? description)
    {
        var response = await _http.PostAsJsonAsync("api/projects", new { name, description });
        return await ReadDataAsync<ProjectModel>(response);
    }

    public async Task<ProjectModel?> UpdateProjectAsync(int id, string name, string? description)
    {
        var response = await _http.PutAsJsonAsync($"api/projects/{id}", new { name, description });
        return await ReadDataAsync<ProjectModel>(response);
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        LastError = null;
        var response = await _http.DeleteAsync($"api/projects/{id}");
        if (!response.IsSuccessStatusCode)
        {
            LastError = $"Delete failed ({(int)response.StatusCode})";
            return false;
        }
        return true;
    }

    // ── Tasks ─────────────────────────────────────────────────────────────────

    public async Task<List<TaskModel>?> GetTasksAsync(int projectId)
    {
        var response = await _http.GetAsync($"api/projects/{projectId}/tasks");
        return await ReadDataAsync<List<TaskModel>>(response);
    }

    public async Task<TaskModel?> GetTaskAsync(int projectId, int id)
    {
        var response = await _http.GetAsync($"api/projects/{projectId}/tasks/{id}");
        return await ReadDataAsync<TaskModel>(response);
    }

    public async Task<TaskModel?> CreateTaskAsync(
        int projectId, string title, string? description,
        string status, string priority, DateTime? dueDate)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/projects/{projectId}/tasks",
            new { title, description, status, priority, dueDate });
        return await ReadDataAsync<TaskModel>(response);
    }

    public async Task<TaskModel?> UpdateTaskAsync(
        int projectId, int id, string title, string? description,
        string status, string priority, DateTime? dueDate)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/projects/{projectId}/tasks/{id}",
            new { title, description, status, priority, dueDate });
        return await ReadDataAsync<TaskModel>(response);
    }

    public async Task<bool> DeleteTaskAsync(int projectId, int id)
    {
        LastError = null;
        var response = await _http.DeleteAsync($"api/projects/{projectId}/tasks/{id}");
        if (!response.IsSuccessStatusCode)
        {
            LastError = $"Delete failed ({(int)response.StatusCode})";
            return false;
        }
        return true;
    }
}
