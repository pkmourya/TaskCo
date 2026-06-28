using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Web.Services;

namespace TaskCo.Web.Pages.Projects;

public class DeleteModel : PageModel
{
    private readonly ApiClient _client;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public string ProjectName { get; private set; } = "";
    public string? ProjectDescription { get; private set; }
    public string? Error { get; private set; }

    public DeleteModel(ApiClient client) => _client = client;

    public async Task<IActionResult> OnGetAsync()
    {
        var project = await _client.GetProjectAsync(Id);
        if (project == null) return RedirectToPage("/Projects/Index");
        ProjectName = project.Name;
        ProjectDescription = project.Description;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var success = await _client.DeleteProjectAsync(Id);
        if (!success)
        {
            var project = await _client.GetProjectAsync(Id);
            ProjectName = project?.Name ?? "";
            ProjectDescription = project?.Description;
            Error = _client.LastError ?? "Delete failed";
            return Page();
        }
        return RedirectToPage("/Projects/Index");
    }
}
