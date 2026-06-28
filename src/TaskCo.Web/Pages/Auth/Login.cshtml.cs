using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskCo.Web.Services;

namespace TaskCo.Web.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly ApiClient _client;

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? Error { get; set; }

    public LoginModel(ApiClient client) => _client = client;

    public IActionResult OnGet()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken")))
            return RedirectToPage("/Projects/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var token = await _client.LoginAsync(Email, Password);
        if (token == null)
        {
            Error = _client.LastError ?? "Login failed";
            return Page();
        }
        HttpContext.Session.SetString("JwtToken", token);
        return RedirectToPage("/Projects/Index");
    }
}
