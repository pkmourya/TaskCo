using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Web.Services;

namespace TaskCo.Web.Pages.Tasks;

public class CreateModel : PageModel
{
    private readonly ApiClient _client;

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

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

    public CreateModel(ApiClient client) => _client = client;

    public async Task<IActionResult> OnGetAsync()
    {
        var project = await _client.GetProjectAsync(ProjectId);
        if (project == null) return RedirectToPage("/Projects/Index");
        ProjectName = project.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var task = await _client.CreateTaskAsync(ProjectId, Title, Description, Status, Priority, DueDate);
        if (task == null)
        {
            var project = await _client.GetProjectAsync(ProjectId);
            ProjectName = project?.Name;
            Error = _client.LastError ?? "Failed to create task";
            return Page();
        }
        return RedirectToPage("/Tasks/Index", new { projectId = ProjectId });
    }
}
