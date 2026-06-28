using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Api.Exceptions;
using TaskCo.Api.Models.Dtos.Auth;
using TaskCo.Api.Services.Interfaces;

namespace TaskCo.Api.Pages;

public class LoginModel : PageModel
{
    private readonly IAuthService _authService;

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? Error { get; set; }
    public string? EmailError { get; set; }
    public string? PasswordError { get; set; }

    public LoginModel(IAuthService authService) => _authService = authService;

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
            EmailError = "Email is required";
        if (string.IsNullOrWhiteSpace(Password))
            PasswordError = "Password is required";
        if (EmailError != null || PasswordError != null)
            return Page();

        try
        {
            var result = await _authService.LoginAsync(new LoginRequest { Email = Email, Password = Password });
            await IssueCookieAsync(result);
            return RedirectToPage("/Index");
        }
        catch (AppException ex)
        {
            Error = ex.Message;
            return Page();
        }
    }

    private async Task IssueCookieAsync(AuthResponse result)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new(ClaimTypes.Email, result.Email),
            new(ClaimTypes.Name, result.Email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
