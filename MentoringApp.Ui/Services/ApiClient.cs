using System.Net.Http.Json;
using MentoringApp.Api.DTOs.Auth;
using MentoringApp.Api.DTOs.Profiles;

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
            return await response.Content.ReadFromJsonAsync<AuthResponseDto>()!;
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

        public async Task UpsertProfileAsync(UpsertProfileDto dto)
        {
            await PostJsonAsync("/api/profile", dto);
        }
        #endregion profile

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

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent content = null)
        {
            using var request = new HttpRequestMessage(method, url) {  Content = content };

            // forward Authorization and Cookie headers from the current HTTP context if available.
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                if (ctx.Request.Headers.TryGetValue("Authorization", out var authValues) && !string.IsNullOrEmpty(authValues))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", (string)authValues);
                }

                if (ctx.Request.Headers.TryGetValue("Cookie", out var cookieValues) && !string.IsNullOrEmpty(cookieValues))
                {
                    request.Headers.TryAddWithoutValidation("Cookie", (string)cookieValues);
                }
            }

            return await _http.SendAsync(request);
        }
        #endregion helpers

        #endregion endpoints
    }
}
