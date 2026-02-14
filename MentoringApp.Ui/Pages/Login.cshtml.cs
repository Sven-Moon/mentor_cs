using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MentoringApp.Ui.Pages;

public class LoginModel : PageModel
{
    private readonly HttpClient _httpClient;

    public LoginModel(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Api");
    }

    #region Properties

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string RegisterEmail { get; set; } = "";
    [BindProperty] public string RegisterPassword { get; set; } = "";
    [BindProperty] public string ConfirmPassword { get; set; } = "";
    [BindProperty] public string FormType { get; set; } = "";
    public string? Error { get; set; }

    #endregion properties


    #region Public Methods
    public async Task<IActionResult> OnPostAsync()
    {
        return FormType switch
        {
            "login" => await HandleLogin(),
            "register" => await HandleRegister(),
            _ => Page()
        };
    }
    #endregion public methods

    #region Private Methods
    private async Task<IActionResult> HandleLogin()
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


        TempData["Jwt"] = loginResult.Token; // only works for one redirect
        return RedirectToPage("/Index");
    }

    private async Task<IActionResult> HandleRegister()
    {
        if (RegisterPassword != ConfirmPassword)
        {
            Error = "Passwords do not match";
            return Page();
        }

        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = RegisterEmail,
                password = RegisterPassword
            });

        if (!response.IsSuccessStatusCode)
        {
            Error = "Registration failed";
            return Page();
        }

        // Optional UX choice:
        // auto-login later, but for now:
        return RedirectToPage("/Login");
    }
    #endregion private methods

    public record LoginResponse(string Token);
}
