using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskCo.Api.Models.Common;
using TaskCo.Api.Models.Dtos.Projects;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IValidator<CreateProjectRequest> _createValidator;
    private readonly IValidator<UpdateProjectRequest> _updateValidator;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public ProjectsController(
        IProjectService projectService,
        IValidator<CreateProjectRequest> createValidator,
        IValidator<UpdateProjectRequest> updateValidator)
    {
        _projectService = projectService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _projectService.GetAllAsync(CurrentUserId);
        return Ok(ApiResponse.Success(projects));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await _projectService.GetByIdAsync(id, CurrentUserId);
        return Ok(ApiResponse.Success(project));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Failure("validation_error", "Validation failed",
                validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })));

        var project = await _projectService.CreateAsync(request, CurrentUserId);
        return StatusCode(201, ApiResponse.Success(project));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProjectRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Failure("validation_error", "Validation failed",
                validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })));

        var project = await _projectService.UpdateAsync(id, request, CurrentUserId);
        return Ok(ApiResponse.Success(project));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _projectService.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }
}
