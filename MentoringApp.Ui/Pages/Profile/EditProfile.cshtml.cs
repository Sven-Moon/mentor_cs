using MentoringApp.Api.DTOs.Profiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Http.Headers;

namespace MentoringApp.Ui.Pages.Profile
{
    public class EditProfileModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public EditProfileModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Api");
        }

        #region Properties
        [BindProperty] public string FirstName { get; set; } = "";
        [BindProperty] public string LastName { get; set; } = "";
        [BindProperty] public string Bio { get; set; } = "";
        [BindProperty] public string Location { get; set; } = "";

        public string? Error { get; set; }

        #endregion properties

        #region OnGet
        public async Task<IActionResult> OnPostAsync()
        {
            var jwt = TempData["Jwt"] as string; // only works for one redirect
            // replace with Cookie auth or A session-backed token store

            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToPage("/Login");
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/profile",
                new EditProfileDto
                {
                    FirstName = FirstName,
                    LastName = LastName,
                    Bio = Bio,
                    Location = Location
                });

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                Error = "You already have a profile.";
                return Page();
            }

            if (!response.IsSuccessStatusCode)
            {
                Error = "Failed to create profile.";
                return Page();
            }

            return RedirectToPage("/Index");
        }
        #endregion OnGet
    }
}
