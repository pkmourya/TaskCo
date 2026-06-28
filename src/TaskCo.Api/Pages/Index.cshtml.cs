using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Api.Exceptions;
using TaskCo.Api.Models.Dtos.Projects;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Api.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IProjectService _projectService;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public List<ProjectResponse> Projects { get; private set; } = new();

    // create-form state preserved across validation failures
    public bool ShowCreateForm { get; private set; }
    public string? CreateName { get; private set; }
    public string? CreateDescription { get; private set; }
    public string? CreateError { get; private set; }
    public string? NameError { get; private set; }

    public IndexModel(IProjectService projectService) => _projectService = projectService;

    public async Task OnGetAsync()
    {
        Projects = (await _projectService.GetAllAsync(CurrentUserId)).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var name = Request.Form["Name"].ToString().Trim();
        var description = Request.Form["Description"].ToString().Trim();

        ShowCreateForm = true;
        CreateName = name;
        CreateDescription = string.IsNullOrEmpty(description) ? null : description;

        if (string.IsNullOrEmpty(name))
        {
            NameError = "Name is required";
            Projects = (await _projectService.GetAllAsync(CurrentUserId)).ToList();
            return Page();
        }

        try
        {
            await _projectService.CreateAsync(
                new CreateProjectRequest { Name = name, Description = CreateDescription },
                CurrentUserId);
            return RedirectToPage();
        }
        catch (AppException ex)
        {
            CreateError = ex.Message;
            Projects = (await _projectService.GetAllAsync(CurrentUserId)).ToList();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteProjectAsync(int projectId)
    {
        try
        {
            await _projectService.DeleteAsync(projectId, CurrentUserId);
        }
        catch (AppException) { /* swallow – already gone or not owned */ }
        return RedirectToPage();
    }
}
