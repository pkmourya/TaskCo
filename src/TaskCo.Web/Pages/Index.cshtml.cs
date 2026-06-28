using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TaskCo.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken")))
            return RedirectToPage("/Projects/Index");
        return RedirectToPage("/Auth/Login");
    }
}
