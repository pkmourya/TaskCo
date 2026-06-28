using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Web.Models;
using TaskCo.Web.Services;

namespace TaskCo.Web.Pages.Tasks;

public class IndexModel : PageModel
{
    private readonly ApiClient _client;

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    public string? ProjectName { get; private set; }
    public List<TaskModel>? Tasks { get; private set; }
    public string? Error { get; private set; }

    public IndexModel(ApiClient client) => _client = client;

    public async Task<IActionResult> OnGetAsync()
    {
        var project = await _client.GetProjectAsync(ProjectId);
        if (project == null) return RedirectToPage("/Projects/Index");
        ProjectName = project.Name;

        Tasks = await _client.GetTasksAsync(ProjectId);
        if (Tasks == null)
            Error = _client.LastError;

        return Page();
    }
}
