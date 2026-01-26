namespace MentoringApp.Api.DTOs.Auth
{
    public class AuthResponseDto
    {
        public required string UserId { get; set; }
        public required string Email { get; set; }
        public required string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
