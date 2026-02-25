using MentoringApp.Ui.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MentoringApp.Api.DTOs.Profiles;

namespace MentoringApp.Ui.Pages.Profile;

public class IndexModel : PageModel
{
    public IndexModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    #region Properties

    private readonly ApiClient _apiClient;

    // Use the API DTO type (from MentoringApp.Api.DTOs.Profiles) to avoid the implicit conversion error.
    public ProfileDto Profile { get; set; } = new ProfileDto { 
        FirstName = "", 
        LastName = "", 
        Location = "", 
        UserId = "", 
        Bio = ""
    };

    [BindProperty]
    public string EditValue { get; set; } = string.Empty;

    [BindProperty]
    public string FieldName { get; set; } = string.Empty;

    public string EditingField { get; set; } = string.Empty;

    #endregion properties

    public async Task OnGetAsync()
    {
        Profile = await _apiClient.GetMyProfileAsync();
    }

    public async Task<IActionResult> OnPostEditAsync(string field)
    {
        EditingField = field;

        Profile = await _apiClient.GetMyProfileAsync();

        EditValue = field switch
        {
            nameof(Profile.FirstName) => Profile?.FirstName ?? "Unknown User",
            nameof(Profile.LastName) => Profile?.LastName ?? string.Empty,
            nameof(Profile.Bio) => Profile?.Bio ?? string.Empty,
            nameof(Profile.Location) => Profile?.Location ?? string.Empty,
            _ => string.Empty
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        var current = await _apiClient.GetMyProfileAsync();

        if (current is null) return RedirectToPage();
        var updated = new UpdateProfileDto
        {
            FirstName = FieldName == nameof(Profile.FirstName) ? EditValue! : current.FirstName,
            LastName = FieldName == nameof(Profile.LastName) ? EditValue! : current.LastName,
            Bio = FieldName == nameof(Profile.Bio) ? EditValue! : current.Bio,
            Location = FieldName == nameof(Profile.Location) ? EditValue! : current.Location
        };
        await _apiClient.UpdateProfileAsync(updated);

        return RedirectToPage();
    }

    public IActionResult OnPostCancel()
    {
        return RedirectToPage();
    }
}
