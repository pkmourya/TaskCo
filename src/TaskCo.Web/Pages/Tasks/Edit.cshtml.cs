using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Web.Services;

namespace TaskCo.Web.Pages.Tasks;

public class EditModel : PageModel
{
    private readonly ApiClient _client;

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public string Title { get; set; } = "";

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public string Status { get; set; } = "Todo";

    [BindProperty]
    public string Priority { get; set; } = "Medium";

    [BindProperty]
    public DateTime? DueDate { get; set; }

    public string? ProjectName { get; private set; }
    public string? Error { get; private set; }

    public EditModel(ApiClient client) => _client = client;

    public async Task<IActionResult> OnGetAsync()
    {
        var project = await _client.GetProjectAsync(ProjectId);
        if (project == null) return RedirectToPage("/Projects/Index");
        ProjectName = project.Name;

        var task = await _client.GetTaskAsync(ProjectId, Id);
        if (task == null) return RedirectToPage("/Tasks/Index", new { projectId = ProjectId });

        Title = task.Title;
        Description = task.Description;
        Status = task.Status;
        Priority = task.Priority;
        DueDate = task.DueDate;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var task = await _client.UpdateTaskAsync(ProjectId, Id, Title, Description, Status, Priority, DueDate);
        if (task == null)
        {
            var project = await _client.GetProjectAsync(ProjectId);
            ProjectName = project?.Name;
            Error = _client.LastError ?? "Failed to update task";
            return Page();
        }
        return RedirectToPage("/Tasks/Index", new { projectId = ProjectId });
    }
}
