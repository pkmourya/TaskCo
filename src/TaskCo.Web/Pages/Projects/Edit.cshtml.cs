using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Web.Services;

namespace TaskCo.Web.Pages.Projects;

public class EditModel : PageModel
{
    private readonly ApiClient _client;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public string Name { get; set; } = "";

    [BindProperty]
    public string? Description { get; set; }

    public string? Error { get; set; }

    public EditModel(ApiClient client) => _client = client;

    public async Task<IActionResult> OnGetAsync()
    {
        var project = await _client.GetProjectAsync(Id);
        if (project == null) return RedirectToPage("/Projects/Index");
        Name = project.Name;
        Description = project.Description;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var project = await _client.UpdateProjectAsync(Id, Name, Description);
        if (project == null)
        {
            Error = _client.LastError ?? "Failed to update project";
            return Page();
        }
        return RedirectToPage("/Projects/Index");
    }
}
