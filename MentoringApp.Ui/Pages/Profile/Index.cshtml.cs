using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MentoringApp.Ui.Pages.Profile;

public class IndexModel : PageModel
{
    public IndexModel(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("Api");
    }

    #region Properties

    private readonly HttpClient _httpClient;

    public ProfileDto Profile { get; set; } = new();

    [BindProperty]
    public string? EditValue { get; set; }

    [BindProperty]
    public string? FieldName { get; set; }

    public string? EditingField { get; set; }

    #endregion properties

    public async Task OnGetAsync()
    {
        Profile = await _httpClient.GetFromJsonAsync<ProfileDto>("api/profile/me")
                  ?? new ProfileDto();
    }

    public async Task<IActionResult> OnPostEditAsync(string field)
    {
        EditingField = field;

        Profile = await _httpClient.GetFromJsonAsync<ProfileDto>("api/profile/me")
                  ?? new ProfileDto();

        EditValue = field switch
        {
            nameof(Profile.FirstName) => Profile.FirstName,
            nameof(Profile.LastName) => Profile.LastName,
            nameof(Profile.Bio) => Profile.Bio,
            nameof(Profile.Location) => Profile.Location,
            _ => ""
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        var current = await _httpClient.GetFromJsonAsync<ProfileDto>("api/profile/me");

        if (current is null) return RedirectToPage();

        var updated = new EditProfileDto
        {
            FirstName = FieldName == nameof(Profile.FirstName) ? EditValue! : current.FirstName,
            LastName = FieldName == nameof(Profile.LastName) ? EditValue! : current.LastName,
            Bio = FieldName == nameof(Profile.Bio) ? EditValue! : current.Bio,
            Location = FieldName == nameof(Profile.Location) ? EditValue! : current.Location
        };

        await _httpClient.PutAsJsonAsync("api/profile", updated);

        return RedirectToPage();
    }
}

#region DTOs
public class ProfileDto
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Location { get; set; } = "";
}

public class EditProfileDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string Bio { get; set; } = "";
    public string Location { get; set; } = "";
}
#endregion dtos