using MentoringApp.Api.DTOs.Auth;
using MentoringApp.Api.DTOs.Profiles;
using MentoringApp.Api.DTOs.Skills;
using MentoringApp.Api.DTOs.UserSkills;

namespace MentoringApp.Ui.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
        {
            _http = http;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Endpoints

        #region Auth
        public async Task RegisterAsync(RegisterRequestDto dto)
        {
            await PostJsonAsync("/api/auth/register", dto);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var response = await PostAsync("/api/auth/login", dto);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? throw new ApiException(500, "Failed to deserialize login response");
        }

        public async Task LogoutAsync()
        {
            await PostAsync("/api/auth/logout");
        }
        #endregion auth

        #region Profile
        public async Task<ProfileDto> GetMyProfileAsync()
        {
            return await GetJsonAsync<ProfileDto>("/api/profile/me");
        }

        public async Task<ProfileDto?> GetUserProfileAsync(int profileId)
        {
            return await GetJsonAsync<ProfileDto>($"/api/profile/{profileId}");
        }

        public async Task UpdateProfileAsync(UpdateProfileDto dto)
        {
            await PostJsonAsync("/api/profile", dto);
        }
        #endregion profile

        #region Skills
        public async Task<List<SkillResponseDto>> GetAllSkillsAsync(bool approvedOnly = false)
        {
            return await GetJsonAsync<List<SkillResponseDto>>($"/api/skills?approvedOnly={approvedOnly}");
        }

        public async Task<List<SkillCategoryDto>> GetCategoriesAsync()
        {
            return await GetJsonAsync<List<SkillCategoryDto>>("/api/skills/categories");
        }

        public async Task<SkillResponseDto> CreateSkillAsync(SkillCreateDto dto)
        {
            return await PostJsonResultAsync<SkillCreateDto, SkillResponseDto>("/api/skills", dto);
        }
        #endregion skills

        #region UserSkills
        public async Task<List<MentoringApp.Api.DTOs.UserSkills.UserSkillDto>> GetMySkillsAsync()
        {
            return await GetJsonAsync<List<MentoringApp.Api.DTOs.UserSkills.UserSkillDto>>("/api/users/me/skills");
        }

        public async Task AddUserSkillAsync(MentoringApp.Api.DTOs.UserSkills.AddUserSkillDto dto)
        {
            await PostJsonAsync("/api/users/me/skills", dto);
        }

        public async Task UpdateUserSkillAsync(int skillId, UpdateUserSkillDto dto)
        {
            await PutJsonAsync($"/api/users/me/skills/{skillId}", dto);
        }

        public async Task RemoveUserSkillAsync(int skillId)
        {
            await DeleteAsync($"/api/users/me/skills/{skillId}");
        }
        #endregion userSkills

        #region AdminSkills
        public async Task<List<SkillResponseDto>> GetPendingSkillsAsync()
        {
            return await GetJsonAsync<List<SkillResponseDto>>("/api/admin/skills/pending");
        }

        public async Task ApproveSkillAsync(int skillId)
        {
            await PostAsync($"/api/admin/skills/{skillId}/approve");
        }

        public async Task ApproveSkillWithEditsAsync(int skillId, SkillModerationDto dto)
        {
            await PutJsonAsync($"/api/admin/skills/{skillId}/approve", dto);
        }

        public async Task RejectSkillAsync(int skillId)
        {
            await PostAsync($"/api/admin/skills/{skillId}/reject");
        }

        public async Task MarkSkillAsDuplicateAsync(int skillId, int existingSkillId)
        {
            await PostJsonAsync($"/api/admin/skills/{skillId}/duplicate", new MarkDuplicateDto { ExistingSkillId = existingSkillId });
        }
        #endregion adminSkills

        #region Helpers

        private async Task<T> GetJsonAsync<T>(string url)
        {
            var response = await SendAsync(HttpMethod.Get, url);
            await EnsureSuccess(response);
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        private async Task PostJsonAsync<TDto>(string url, TDto dto)
        {
            var response = await PostAsync(url, dto);
            await EnsureSuccess(response);
        }

        private async Task<TResult> PostJsonResultAsync<TDto, TResult>(string url, TDto dto)
        {
            var response = await PostAsync(url, dto);
            await EnsureSuccess(response);
            return (await response.Content.ReadFromJsonAsync<TResult>())!;
        }

        private async Task PutJsonAsync<TDto>(string url, TDto dto)
        {
            var content = JsonContent.Create(dto);
            var response = await SendAsync(HttpMethod.Put, url, content);
            await EnsureSuccess(response);
        }

        private async Task DeleteAsync(string url)
        {
            var response = await SendAsync(HttpMethod.Delete, url);
            await EnsureSuccess(response);
        }

        private async Task<HttpResponseMessage> PostAsync<TDto>(string url, TDto dto)
        {
            var content = JsonContent.Create(dto);
            return await SendAsync(HttpMethod.Post, url, content);
        }

        private async Task<HttpResponseMessage> PostAsync(string url)
        {
            return await SendAsync(HttpMethod.Post, url, null);
        }

        private static async Task EnsureSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var content = await response.Content.ReadAsStringAsync();
            throw new ApiException((int)response.StatusCode, content);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content = null)
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            // Forward JWT from session as Authorization header.
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                var token = ctx.Session.GetString("Jwt");
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                }
            }

            return await _http.SendAsync(request);
        }
        #endregion helpers

        #endregion endpoints
    }
}
