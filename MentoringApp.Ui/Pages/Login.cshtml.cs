using MentoringApp.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MentoringApp.Ui.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    public string? Error { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _signInManager.PasswordSignInAsync(
            Email, Password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            Error = "Invalid login";
            return Page();
        }

        return RedirectToPage("/Index");
    }
}
