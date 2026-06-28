using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Web.Services;

namespace TaskCo.Web.Pages.Tasks;

public class DeleteModel : PageModel
{
    private readonly ApiClient _client;

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public string TaskTitle { get; private set; } = "";
    public string? TaskDescription { get; private set; }
    public string? Error { get; private set; }

    public DeleteModel(ApiClient client) => _client = client;

    public async Task<IActionResult> OnGetAsync()
    {
        var task = await _client.GetTaskAsync(ProjectId, Id);
        if (task == null) return RedirectToPage("/Tasks/Index", new { projectId = ProjectId });
        TaskTitle = task.Title;
        TaskDescription = task.Description;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var success = await _client.DeleteTaskAsync(ProjectId, Id);
        if (!success)
        {
            var task = await _client.GetTaskAsync(ProjectId, Id);
            TaskTitle = task?.Title ?? "";
            TaskDescription = task?.Description;
            Error = _client.LastError ?? "Delete failed";
            return Page();
        }
        return RedirectToPage("/Tasks/Index", new { projectId = ProjectId });
    }
}
