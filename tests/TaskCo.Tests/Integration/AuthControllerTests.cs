using System.Net;
using System.Net.Http.Json;
using TaskCo.Api.Models.Dtos.Auth;
using TaskCo.Tests.Helpers;

namespace TaskCo.Tests.Integration;

public class AuthControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "user@example.com",
            password = "Password123"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await ApiHelpers.ReadDataAsync<AuthResponse>(response);
        Assert.NotNull(data);
        Assert.NotEmpty(data.Token);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409WithConflictCode()
    {
        var payload = new { email = "dupe@example.com", password = "Password123" };
        await _client.PostAsJsonAsync("/api/auth/register", payload);

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("conflict", code);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400WithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "Password123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("validation_error", code);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400WithValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "user@example.com",
            password = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("validation_error", code);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "user@example.com",
            password = "Password123"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "user@example.com",
            password = "Password123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await ApiHelpers.ReadDataAsync<AuthResponse>(response);
        Assert.NotNull(data);
        Assert.NotEmpty(data.Token);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401WithUnauthorizedCode()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "user@example.com",
            password = "Password123"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "user@example.com",
            password = "WrongPassword"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("unauthorized", code);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401WithUnauthorizedCode()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@example.com",
            password = "Password123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var (code, _) = await ApiHelpers.ReadErrorAsync(response);
        Assert.Equal("unauthorized", code);
    }
}
