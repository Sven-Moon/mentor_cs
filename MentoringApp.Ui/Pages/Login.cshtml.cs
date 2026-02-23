using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        if (_httpClient.BaseAddress is null)
        {
            Error = "API base address is not configured. Verify your HttpClient registration for the named client \"Api\".";
            return Page();
        }

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email = Email,
                    password = Password
                });

            if (!response.IsSuccessStatusCode)
            {
                // Read server response body for diagnostics (may include stack trace or error details)
                string serverBody = string.Empty;
                try
                {
                    serverBody = await response.Content.ReadAsStringAsync();
                }
                catch
                {
                    // Ignore errors reading the body - we still want to surface status code.
                }

                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    // Provide a helpful message but include server details to troubleshoot.
                    Error = $"Server error (500). Response: {serverBody}";
                }
                else
                {
                    Error = $"Login failed ({(int)response.StatusCode}). Response: {serverBody}";
                }

                return Page();
            }

            var loginResult = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (loginResult is null || string.IsNullOrEmpty(loginResult.Token))
            {
                Error = "Login failed";
                return Page();
            }

            HttpContext.Session.SetString("Jwt", loginResult.Token); // store access token in session

            CreateClaimsAndSignIn();

            return RedirectToPage("/Index");
        }
        catch (HttpRequestException ex)
        {
            // Typical "actively refused" or DNS/failure to connect scenarios surface here.
            Error = "Unable to reach the authentication server. Please ensure the API is running and the base URL is correct. " + ex.Message;
            return Page();
        }
        catch (TaskCanceledException ex)
        {
            // Timeout
            Error = "The request timed out. " + ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            // Fallback
            Error = "Unexpected error while attempting to log in. " + ex.Message;
            return Page();
        }
    }


    private async void CreateClaimsAndSignIn()
    {
        var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Email),
                    new Claim(ClaimTypes.Email, Email)
                };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
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
            string serverBody = string.Empty;
            try
            {
                serverBody = await response.Content.ReadAsStringAsync();
            }
            catch { }

            Error = $"Registration failed ({(int)response.StatusCode}). {serverBody}";
            return Page();
        }

        // Optional UX choice:
        // auto-login later, but for now:
        return RedirectToPage("/Login");
    }
    #endregion private methods

    public record LoginResponse(string Token);
}
