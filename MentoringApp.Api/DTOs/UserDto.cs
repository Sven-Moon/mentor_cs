namespace MentoringApp.Api.DTOs
{
    public class UserDto
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public string Role { get; set; } = "User"; // Default role is User, not Admin

        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
    }
}
