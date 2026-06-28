using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskCo.Api.Models.Common;
using TaskCo.Api.Models.Dtos.Tasks;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/tasks")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TaskItemsController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public TaskItemsController(
        ITaskService taskService,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator)
    {
        _taskService = taskService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int projectId)
    {
        var tasks = await _taskService.GetAllAsync(projectId, CurrentUserId);
        return Ok(ApiResponse.Success(tasks));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int projectId, int id)
    {
        var task = await _taskService.GetByIdAsync(projectId, id, CurrentUserId);
        return Ok(ApiResponse.Success(task));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int projectId, CreateTaskRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Failure("validation_error", "Validation failed",
                validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })));

        var task = await _taskService.CreateAsync(projectId, request, CurrentUserId);
        return StatusCode(201, ApiResponse.Success(task));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int projectId, int id, UpdateTaskRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Failure("validation_error", "Validation failed",
                validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })));

        var task = await _taskService.UpdateAsync(projectId, id, request, CurrentUserId);
        return Ok(ApiResponse.Success(task));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int projectId, int id)
    {
        await _taskService.DeleteAsync(projectId, id, CurrentUserId);
        return NoContent();
    }
}
