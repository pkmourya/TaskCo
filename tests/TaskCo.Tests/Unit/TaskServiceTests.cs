using Microsoft.EntityFrameworkCore;
using TaskCo.Api.Data;
using TaskCo.Api.Exceptions;
using TaskCo.Api.Models.Dtos.Tasks;
using TaskCo.Api.Models.Entities;
using TaskCo.Api.Services;

namespace TaskCo.Tests.Unit;

public class TaskServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppDbContext db, User user1, User user2, Project p1, Project p2)> SeedAsync()
    {
        var db = CreateDb();
        var user1 = new User { Email = "u1@x.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Email = "u2@x.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var p1 = new Project { Name = "U1 Project", OwnerId = user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var p2 = new Project { Name = "U2 Project", OwnerId = user2.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Projects.AddRange(p1, p2);
        await db.SaveChangesAsync();

        return (db, user1, user2, p1, p2);
    }

    [Fact]
    public async Task GetAllAsync_ValidProject_ReturnsTasks()
    {
        var (db, user1, _, p1, _) = await SeedAsync();
        db.TaskItems.AddRange(
            new TaskItem { Title = "T1", ProjectId = p1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TaskItem { Title = "T2", ProjectId = p1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = (await new TaskService(db).GetAllAsync(p1.Id, user1.Id)).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_NonExistentProject_ThrowsNotFoundException()
    {
        var (db, user1, _, _, _) = await SeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new TaskService(db).GetAllAsync(999, user1.Id));
    }

    [Fact]
    public async Task GetAllAsync_AnotherUsersProject_ThrowsNotFoundException()
    {
        var (db, _, user2, p1, _) = await SeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new TaskService(db).GetAllAsync(p1.Id, user2.Id));
    }

    [Fact]
    public async Task GetByIdAsync_OwnedTask_ReturnsTask()
    {
        var (db, user1, _, p1, _) = await SeedAsync();
        var task = new TaskItem { Title = "My Task", ProjectId = p1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        var result = await new TaskService(db).GetByIdAsync(p1.Id, task.Id, user1.Id);

        Assert.Equal("My Task", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_AnotherUsersTask_ThrowsNotFoundException()
    {
        var (db, user1, user2, p1, _) = await SeedAsync();
        var task = new TaskItem { Title = "User1 Task", ProjectId = p1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new TaskService(db).GetByIdAsync(p1.Id, task.Id, user2.Id));
    }

    [Fact]
    public async Task CreateAsync_DefaultsStatusAndPriority()
    {
        var (db, user1, _, p1, _) = await SeedAsync();

        var result = await new TaskService(db).CreateAsync(p1.Id,
            new CreateTaskRequest { Title = "New Task" },
            user1.Id);

        Assert.Equal(TaskItemStatus.Todo, result.Status);
        Assert.Equal(TaskItemPriority.Medium, result.Priority);
    }

    [Fact]
    public async Task CreateAsync_CustomStatusAndPriority_Applied()
    {
        var (db, user1, _, p1, _) = await SeedAsync();

        var result = await new TaskService(db).CreateAsync(p1.Id,
            new CreateTaskRequest { Title = "Task", Status = TaskItemStatus.InProgress, Priority = TaskItemPriority.High },
            user1.Id);

        Assert.Equal(TaskItemStatus.InProgress, result.Status);
        Assert.Equal(TaskItemPriority.High, result.Priority);
    }

    [Fact]
    public async Task CreateAsync_AnotherUsersProject_ThrowsNotFoundException()
    {
        var (db, _, user2, p1, _) = await SeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new TaskService(db).CreateAsync(p1.Id,
                new CreateTaskRequest { Title = "Hijack" },
                user2.Id));
    }

    [Fact]
    public async Task UpdateAsync_OwnedTask_UpdatesFields()
    {
        var (db, user1, _, p1, _) = await SeedAsync();
        var task = new TaskItem { Title = "Old", ProjectId = p1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        var result = await new TaskService(db).UpdateAsync(p1.Id, task.Id,
            new UpdateTaskRequest { Title = "New", Status = TaskItemStatus.Done, Priority = TaskItemPriority.Low },
            user1.Id);

        Assert.Equal("New", result.Title);
        Assert.Equal(TaskItemStatus.Done, result.Status);
        Assert.Equal(TaskItemPriority.Low, result.Priority);
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersTask_ThrowsNotFoundException()
    {
        var (db, user1, user2, p1, _) = await SeedAsync();
        var task = new TaskItem { Title = "Task", ProjectId = p1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new TaskService(db).UpdateAsync(p1.Id, task.Id,
                new UpdateTaskRequest { Title = "Hijack", Status = TaskItemStatus.Done, Priority = TaskItemPriority.Low },
                user2.Id));
    }

    [Fact]
    public async Task DeleteAsync_OwnedTask_Removes()
    {
        var (db, user1, _, p1, _) = await SeedAsync();
        var task = new TaskItem { Title = "Task", ProjectId = p1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        await new TaskService(db).DeleteAsync(p1.Id, task.Id, user1.Id);

        Assert.Equal(0, await db.TaskItems.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_AnotherUsersTask_ThrowsNotFoundException()
    {
        var (db, user1, user2, p1, _) = await SeedAsync();
        var task = new TaskItem { Title = "Task", ProjectId = p1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new TaskService(db).DeleteAsync(p1.Id, task.Id, user2.Id));
    }
}
