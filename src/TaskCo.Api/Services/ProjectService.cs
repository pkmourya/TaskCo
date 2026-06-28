using Microsoft.EntityFrameworkCore;
using TaskCo.Api.Data;
using TaskCo.Api.Exceptions;
using TaskCo.Api.Models.Dtos.Projects;
using TaskCo.Api.Models.Entities;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Api.Services;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ProjectResponse>> GetAllAsync(int userId)
    {
        var projects = await _db.Projects
            .Where(p => p.OwnerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(ToResponse);
    }

    public async Task<ProjectResponse> GetByIdAsync(int id, int userId)
    {
        var project = await _db.Projects
            .SingleOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

        if (project is null)
            throw new NotFoundException($"Project {id} not found");

        return ToResponse(project);
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, int userId)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return ToResponse(project);
    }

    public async Task<ProjectResponse> UpdateAsync(int id, UpdateProjectRequest request, int userId)
    {
        var project = await _db.Projects
            .SingleOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

        if (project is null)
            throw new NotFoundException($"Project {id} not found");

        project.Name = request.Name;
        project.Description = request.Description;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ToResponse(project);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        var project = await _db.Projects
            .SingleOrDefaultAsync(p => p.Id == id && p.OwnerId == userId);

        if (project is null)
            throw new NotFoundException($"Project {id} not found");

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
    }

    private static ProjectResponse ToResponse(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
    };
}
