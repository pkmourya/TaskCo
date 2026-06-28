using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Web.Models;
using TaskCo.Web.Services;

namespace TaskCo.Web.Pages.Projects;

public class IndexModel : PageModel
{
    private readonly ApiClient _client;

    public List<ProjectModel>? Projects { get; private set; }
    public string? Error { get; private set; }

    public IndexModel(ApiClient client) => _client = client;

    public async Task OnGetAsync()
    {
        Projects = await _client.GetProjectsAsync();
        if (Projects == null)
            Error = _client.LastError;
    }
}
