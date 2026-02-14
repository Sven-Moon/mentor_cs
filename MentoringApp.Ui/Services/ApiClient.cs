using System.Net.Http.Json;
using MentoringApp.Api.DTOs.Auth;
using MentoringApp.Api.DTOs.Profiles;

namespace MentoringApp.Ui.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;

        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        #region Endpoints

        #region Auth
        public async Task RegisterAsync(RegisterRequestDto dto)
        {
            await PostJsonAsync("/api/auth/register", dto);
        }

        public async Task LoginAsync(LoginRequestDto dto)
        {
            await PostJsonAsync("/api/auth/login", dto);
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

        public async Task EditProfileAsync(EditProfileDto dto)
        {
            await PostJsonAsync("/api/profile", dto);
        }
        #endregion profile

        #region Helpers

        private async Task<T> GetJsonAsync<T>(string url)
        {
            var response = await _http.GetAsync(url);
            await EnsureSuccess(response);
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        private async Task PostJsonAsync<TDto>(string url, TDto dto)
        {
            var response = await _http.PostAsJsonAsync(url, dto);
            await EnsureSuccess(response);
        }

        private async Task PostAsync(string url)
        {
            var response = await _http.PostAsync(url, null);
            await EnsureSuccess(response);
        }

        private static async Task EnsureSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var content = await response.Content.ReadAsStringAsync();
            throw new ApiException((int)response.StatusCode, content);
        }
        #endregion helpers

        #endregion endpoints
    }

}
