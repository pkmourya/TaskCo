namespace TaskCo.Api.Models.Common;

public static class ApiResponse
{
    public static object Success<T>(T data) => new { data };

    public static object Failure(string code, string message, object? details = null) =>
        new { error = new { code, message, details } };
}
