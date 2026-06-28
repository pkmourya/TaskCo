using TaskCo.Api.Models.Dtos.Projects;

namespace TaskCo.Api.Services.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponse>> GetAllAsync(int userId);
    Task<ProjectResponse> GetByIdAsync(int id, int userId);
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, int userId);
    Task<ProjectResponse> UpdateAsync(int id, UpdateProjectRequest request, int userId);
    Task DeleteAsync(int id, int userId);
}
