using TaskCo.Api.Models.Entities;

namespace TaskCo.Api.Models.Dtos.Tasks;

public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public TaskItemPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
}
