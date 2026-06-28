using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Web.Services;

namespace TaskCo.Web.Pages.Projects;

public class CreateModel : PageModel
{
    private readonly ApiClient _client;

    [BindProperty]
    public string Name { get; set; } = "";

    [BindProperty]
    public string? Description { get; set; }

    public string? Error { get; set; }

    public CreateModel(ApiClient client) => _client = client;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var project = await _client.CreateProjectAsync(Name, Description);
        if (project == null)
        {
            Error = _client.LastError ?? "Failed to create project";
            return Page();
        }
        return RedirectToPage("/Projects/Index");
    }
}
