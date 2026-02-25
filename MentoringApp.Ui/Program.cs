using MentoringApp.Api.Data;
using MentoringApp.Ui.Services;
using MentoringApp.Ui.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

#region Services
// Memory cache required by session
builder.Services.AddDistributedMemoryCache();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        //options.LogoutPath = "/Logout"; // middleware intercepts POST /Logout, signs the user out, and redirects to a default location (often the home page).
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// Session
builder.Services.AddSession(
//    options =>
//{     options.IdleTimeout = TimeSpan.FromMinutes(30);
//      options.Cookie.HttpOnly = true;
//      options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//      options.Cookie.SameSite = SameSiteMode.Lax;  }
);

// IHttpContextAccessor for the DelegatingHandler
builder.Services.AddHttpContextAccessor();

#region Registrations

builder.Services.AddScoped<BearerTokenHandler>();
builder.Services.AddScoped<IProfileService, ProfileService>();

#endregion registrations

// ApiClient
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

#endregion services

var app = builder.Build();

#region App Config

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();// Make session available to request handlers and to the DelegatingHandler
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

#endregion app config

app.Run();
