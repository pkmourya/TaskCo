using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Api.Exceptions;
using TaskCo.Api.Models.Dtos.Projects;
using TaskCo.Api.Models.Dtos.Tasks;
using TaskCo.Api.Models.Entities;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Api.Pages.Projects;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public ProjectResponse? Project { get; private set; }
    public List<TaskResponse> Tasks { get; private set; } = new();
    public string? ActiveFilter { get; private set; }

    // create-task form state
    public bool ShowCreateForm { get; private set; }
    public string? CreateTitle { get; private set; }
    public string? CreateDescription { get; private set; }
    public string CreateStatus { get; private set; } = "Todo";
    public string CreatePriority { get; private set; } = "Medium";
    public DateTime? CreateDueDate { get; private set; }
    public string? CreateError { get; private set; }
    public string? TitleError { get; private set; }

    public DetailsModel(IProjectService projectService, ITaskService taskService)
    {
        _projectService = projectService;
        _taskService = taskService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostCreateTaskAsync()
    {
        var title = Request.Form["Title"].ToString().Trim();
        var description = Request.Form["TaskDescription"].ToString().Trim();
        var status = Request.Form["TaskStatus"].ToString();
        var priority = Request.Form["TaskPriority"].ToString();
        _ = DateTime.TryParse(Request.Form["DueDate"].ToString(), out var dueDateParsed);
        DateTime? dueDate = string.IsNullOrEmpty(Request.Form["DueDate"]) ? null : dueDateParsed;

        ShowCreateForm = true;
        CreateTitle = title;
        CreateDescription = string.IsNullOrEmpty(description) ? null : description;
        CreateStatus = status;
        CreatePriority = priority;
        CreateDueDate = dueDate;

        if (string.IsNullOrEmpty(title))
        {
            TitleError = "Title is required";
            return await LoadPageAsync();
        }

        if (!Enum.TryParse<TaskItemStatus>(status, out var taskStatus))
            taskStatus = TaskItemStatus.Todo;
        if (!Enum.TryParse<TaskItemPriority>(priority, out var taskPriority))
            taskPriority = TaskItemPriority.Medium;

        try
        {
            await _taskService.CreateAsync(ProjectId, new CreateTaskRequest
            {
                Title = title,
                Description = CreateDescription,
                Status = taskStatus,
                Priority = taskPriority,
                DueDate = dueDate
            }, CurrentUserId);
            return RedirectToPage(new { projectId = ProjectId, filter = Filter });
        }
        catch (AppException ex)
        {
            CreateError = ex.Message;
            return await LoadPageAsync();
        }
    }

    public async Task<IActionResult> OnPostDeleteTaskAsync(int projectId, int taskId)
    {
        try
        {
            await _taskService.DeleteAsync(projectId, taskId, CurrentUserId);
        }
        catch (AppException) { /* already gone or not owned */ }
        return RedirectToPage(new { projectId = ProjectId, filter = Filter });
    }

    private async Task<IActionResult> LoadPageAsync()
    {
        try
        {
            Project = await _projectService.GetByIdAsync(ProjectId, CurrentUserId);
        }
        catch (NotFoundException)
        {
            return RedirectToPage("/Index");
        }

        var all = (await _taskService.GetAllAsync(ProjectId, CurrentUserId)).ToList();

        ActiveFilter = Filter;

        if (!string.IsNullOrEmpty(Filter) && Enum.TryParse<TaskItemStatus>(Filter, out var filterEnum))
            Tasks = all.Where(t => t.Status == filterEnum).ToList();
        else
            Tasks = all;

        return Page();
    }
}
