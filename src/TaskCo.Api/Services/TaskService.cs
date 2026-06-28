using Microsoft.EntityFrameworkCore;
using TaskCo.Api.Data;
using TaskCo.Api.Exceptions;
using TaskCo.Api.Models.Dtos.Tasks;
using TaskCo.Api.Models.Entities;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Api.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<TaskResponse>> GetAllAsync(int projectId, int userId)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId))
            throw new NotFoundException($"Project {projectId} not found");

        var tasks = await _db.TaskItems
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tasks.Select(ToResponse);
    }

    public async Task<TaskResponse> GetByIdAsync(int projectId, int id, int userId)
    {
        var task = await _db.TaskItems
            .SingleOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId && t.Project.OwnerId == userId);

        if (task is null)
            throw new NotFoundException($"Task {id} not found");

        return ToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(int projectId, CreateTaskRequest request, int userId)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId))
            throw new NotFoundException($"Project {projectId} not found");

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            Status = request.Status ?? TaskItemStatus.Todo,
            Priority = request.Priority ?? TaskItemPriority.Medium,
            DueDate = request.DueDate,
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        return ToResponse(task);
    }

    public async Task<TaskResponse> UpdateAsync(int projectId, int id, UpdateTaskRequest request, int userId)
    {
        var task = await _db.TaskItems
            .SingleOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId && t.Project.OwnerId == userId);

        if (task is null)
            throw new NotFoundException($"Task {id} not found");

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ToResponse(task);
    }

    public async Task DeleteAsync(int projectId, int id, int userId)
    {
        var task = await _db.TaskItems
            .SingleOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId && t.Project.OwnerId == userId);

        if (task is null)
            throw new NotFoundException($"Task {id} not found");

        _db.TaskItems.Remove(task);
        await _db.SaveChangesAsync();
    }

    private static TaskResponse ToResponse(TaskItem t) => new()
    {
        Id = t.Id,
        ProjectId = t.ProjectId,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        Priority = t.Priority,
        DueDate = t.DueDate,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
    };
}
