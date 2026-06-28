using System.Net.Http.Headers;

namespace TaskCo.Web.Services;

public class AuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;

    public AuthHandler(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var ctx = _accessor.HttpContext;
        if (ctx != null)
        {
            await ctx.Session.LoadAsync(cancellationToken);
            var token = ctx.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
