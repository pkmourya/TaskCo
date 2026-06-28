using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TaskCo.Web.Filters;

public class RequireAuthFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        await context.HttpContext.Session.LoadAsync();
        var token = context.HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            context.Result = new RedirectToPageResult("/Auth/Login");
            return;
        }
        await next();
    }
}
