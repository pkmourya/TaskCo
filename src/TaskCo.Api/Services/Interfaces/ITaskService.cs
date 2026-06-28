using TaskCo.Api.Models.Dtos.Tasks;

namespace TaskCo.Api.Services.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponse>> GetAllAsync(int projectId, int userId);
    Task<TaskResponse> GetByIdAsync(int projectId, int id, int userId);
    Task<TaskResponse> CreateAsync(int projectId, CreateTaskRequest request, int userId);
    Task<TaskResponse> UpdateAsync(int projectId, int id, UpdateTaskRequest request, int userId);
    Task DeleteAsync(int projectId, int id, int userId);
}
