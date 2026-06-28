using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskCo.Api.Models.Common;
using TaskCo.Api.Models.Dtos.Auth;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Failure("validation_error", "Validation failed",
                validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })));

        var result = await _authService.RegisterAsync(request);
        return StatusCode(201, ApiResponse.Success(result));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse.Failure("validation_error", "Validation failed",
                validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })));

        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse.Success(result));
    }
}
