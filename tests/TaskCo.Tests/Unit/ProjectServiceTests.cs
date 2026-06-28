using Microsoft.EntityFrameworkCore;
using TaskCo.Api.Data;
using TaskCo.Api.Exceptions;
using TaskCo.Api.Models.Dtos.Projects;
using TaskCo.Api.Models.Entities;
using TaskCo.Api.Services;

namespace TaskCo.Tests.Unit;

public class ProjectServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(AppDbContext db, User user1, User user2)> SeedUsersAsync()
    {
        var db = CreateDb();
        var user1 = new User { Email = "user1@example.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Email = "user2@example.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();
        return (db, user1, user2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyOwnedProjects()
    {
        var (db, user1, user2) = await SeedUsersAsync();
        db.Projects.AddRange(
            new Project { Name = "U1P1", OwnerId = user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Project { Name = "U1P2", OwnerId = user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Project { Name = "U2P1", OwnerId = user2.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new ProjectService(db);
        var results = (await service.GetAllAsync(user1.Id)).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("U1", r.Name));
    }

    [Fact]
    public async Task GetByIdAsync_OwnedProject_ReturnsProject()
    {
        var (db, user1, _) = await SeedUsersAsync();
        var project = new Project { Name = "My Project", OwnerId = user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new ProjectService(db);
        var result = await service.GetByIdAsync(project.Id, user1.Id);

        Assert.Equal("My Project", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_AnotherUsersProject_ThrowsNotFoundException()
    {
        var (db, user1, user2) = await SeedUsersAsync();
        var project = new Project { Name = "User1 Project", OwnerId = user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new ProjectService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(project.Id, user2.Id));
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ThrowsNotFoundException()
    {
        var (db, user1, _) = await SeedUsersAsync();
        var service = new ProjectService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(999, user1.Id));
    }

    [Fact]
    public async Task CreateAsync_SetsOwnerAndTimestamps()
    {
        var (db, user1, _) = await SeedUsersAsync();
        var service = new ProjectService(db);

        var result = await service.CreateAsync(
            new CreateProjectRequest { Name = "New Project", Description = "Desc" },
            user1.Id);

        Assert.Equal("New Project", result.Name);
        Assert.Equal("Desc", result.Description);
        var saved = await db.Projects.FindAsync(result.Id);
        Assert.Equal(user1.Id, saved!.OwnerId);
    }

    [Fact]
    public async Task UpdateAsync_OwnedProject_UpdatesFields()
    {
        var (db, user1, _) = await SeedUsersAsync();
        var project = new Project { Name = "Old Name", OwnerId = user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new ProjectService(db);
        var result = await service.UpdateAsync(project.Id,
            new UpdateProjectRequest { Name = "New Name" },
            user1.Id);

        Assert.Equal("New Name", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersProject_ThrowsNotFoundException()
    {
        var (db, user1, user2) = await SeedUsersAsync();
        var project = new Project { Name = "User1 Project", OwnerId = user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new ProjectService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(project.Id, new UpdateProjectRequest { Name = "Hijack" }, user2.Id));
    }

    [Fact]
    public async Task DeleteAsync_OwnedProject_RemovesProject()
    {
        var (db, user1, _) = await SeedUsersAsync();
        var project = new Project { Name = "To Delete", OwnerId = user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new ProjectService(db);
        await service.DeleteAsync(project.Id, user1.Id);

        Assert.Equal(0, await db.Projects.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_AnotherUsersProject_ThrowsNotFoundException()
    {
        var (db, user1, user2) = await SeedUsersAsync();
        var project = new Project { Name = "User1 Project", OwnerId = user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new ProjectService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DeleteAsync(project.Id, user2.Id));
    }
}
