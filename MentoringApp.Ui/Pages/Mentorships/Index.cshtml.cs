using MentoringApp.Api.DTOs.Mentorship;
using MentoringApp.Ui.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MentoringApp.Ui.Pages.Mentorships;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApiClient _apiClient;

    public IndexModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // Display data
    public List<MentorshipSummary> Mentorships { get; set; } = new();
    public List<UserSummary> Users { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // Create form fields (admin only)
    [BindProperty] public string NewMentorId { get; set; } = "";
    [BindProperty] public string NewMenteeId { get; set; } = "";
    [BindProperty] public string NewScope { get; set; } = "";
    [BindProperty] public string NewStatus { get; set; } = "Active";
    [BindProperty] public DateTime NewStartDate { get; set; } = DateTime.UtcNow.Date;
    [BindProperty] public DateTime NewEndDate { get; set; } = DateTime.UtcNow.Date.AddMonths(6);

    public async Task OnGetAsync()
    {
        await Task.WhenAll(LoadMentorshipsAsync(), LoadUsersAsync());
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            await _apiClient.CreateMentorshipAsync(new MentorshipDto
            {
                MentorId = NewMentorId,
                MenteeId = NewMenteeId,
                Scope = NewScope,
                Status = NewStatus,
                StartDate = NewStartDate,
                EndDate = NewEndDate,
                Version = new byte[4]
            });
            SuccessMessage = "Mentorship created successfully.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.StatusCode == 403
                ? "You do not have permission to create mentorships."
                : $"Could not create mentorship: {ex.Message}";
        }

        await Task.WhenAll(LoadMentorshipsAsync(), LoadUsersAsync());
        return Page();
    }

    private async Task LoadMentorshipsAsync()
    {
        try
        {
            Mentorships = await _apiClient.GetMentorshipsAsync();
        }
        catch (ApiException)
        {
            Mentorships = new List<MentorshipSummary>();
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            Users = await _apiClient.GetAdminUsersAsync();
        }
        catch (ApiException)
        {
            Users = new List<UserSummary>();
        }
    }
}
