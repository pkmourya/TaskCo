namespace TaskCo.Web.Models;

public record AuthResponse(string Token);

public record ProjectModel(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TaskModel(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);
