using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MentoringApp.Api.DTOs.Auth;

namespace MentoringApp.Ui.Pages;

public class LoginModel : PageModel
{
    private readonly HttpClient _httpClient;

    public LoginModel(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Api");
    }

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    public string? Error { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = Email,
                password = Password
            });

        if (!response.IsSuccessStatusCode)
        {
            Error = "Invalid email or password";
            return Page();
        }

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (loginResult is null || string.IsNullOrEmpty(loginResult.Token))
        {
            Error = "Login failed";
            return Page();
        }

        // 🚧 For now: just prove it works
        // Later we’ll store this in a cookie/session
        TempData["Jwt"] = loginResult.Token;

        return RedirectToPage("/Index");
    }

    public record LoginResponse(string Token);
}
