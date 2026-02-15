using MentoringApp.Api.Data;
using MentoringApp.Ui.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Memory cache required by session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(
//    options =>
//{     options.IdleTimeout = TimeSpan.FromMinutes(30);
//      options.Cookie.HttpOnly = true;
//      options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//      options.Cookie.SameSite = SameSiteMode.Lax;  }
);

// IHttpContextAccessor for the DelegatingHandler
builder.Services.AddHttpContextAccessor();

// Register the handler
builder.Services.AddScoped<BearerTokenHandler>();

builder.Services.AddHttpClient<ApiClient>("Api", client =>
{
    // API's https port: 7263
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("Missing configuration: Api:BaseUrl"));
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = new CookieContainer(),
    };
})
.AddHttpMessageHandler<BearerTokenHandler>(); // attach the handler so Authorization header is applied

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(
    builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container: Razor Pages + Identity UI
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        options.Conventions.AllowAnonymousToFolder("/Identity");
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();// Make session available to request handlers and to the DelegatingHandler
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
