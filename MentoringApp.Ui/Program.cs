using MentoringApp.Api.Data;
using MentoringApp.Ui.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<ApiClient>("Api", client =>
{
    // API's https port: 7263
    client.BaseAddress = new Uri("https://localhost:7263");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = new CookieContainer(),
    };
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
