using MentoringApp.Api.DTOs.Profiles;
using MentoringApp.Ui.Services.Interfaces;

namespace MentoringApp.Ui.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApiClient _apiClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProfileService(ApiClient apiClient, IHttpContextAccessor httpContextAccessor)
        {
            _apiClient = apiClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ProfileDto?> GetProfileByIdAsync(int profileId)
        {
            try
            {
                return await _apiClient.GetUserProfileAsync(profileId);
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                return null;
            }
        }

        public async Task<ProfileDto?> GetMyProfileAsync(bool throwOnErrror = true)
        {
            // Return null if user is not authenticated
            if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            try
            {
                return await _apiClient.GetMyProfileAsync();
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                if (throwOnErrror) throw;
                return null; // profile doesn't exist yet
            }
            catch (Exception)
            {
                if (throwOnErrror) throw;
                return null; // error in production
            }
        }

        public async Task UpdateProfileAsync(UpdateProfileDto dto)
        {
            await _apiClient.UpdateProfileAsync(dto);
        }
    }
}
