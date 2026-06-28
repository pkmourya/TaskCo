using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TaskCo.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Auth/Login");

    public IActionResult OnPost()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Auth/Login");
    }
}
