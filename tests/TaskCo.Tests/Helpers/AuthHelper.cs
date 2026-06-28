using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskCo.Api.Models.Dtos.Auth;

namespace TaskCo.Tests.Helpers;

public static class AuthHelper
{
    public static async Task<string> RegisterAndGetTokenAsync(
        HttpClient client,
        string email,
        string password = "Password123")
    {
        await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var data = await ApiHelpers.ReadDataAsync<AuthResponse>(response);
        return data!.Token;
    }

    public static void SetBearerToken(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
}
