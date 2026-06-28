using TaskCo.Web.Filters;
using TaskCo.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".TaskCo.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(1);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthHandler>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured");

builder.Services.AddHttpClient<ApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthHandler>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddFolderApplicationModelConvention("/Projects", model =>
        model.Filters.Add(new RequireAuthFilter()));
    options.Conventions.AddFolderApplicationModelConvention("/Tasks", model =>
        model.Filters.Add(new RequireAuthFilter()));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSession();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
