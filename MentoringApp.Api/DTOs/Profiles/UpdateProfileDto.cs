namespace MentoringApp.Api.DTOs.Profiles
{
    public class UpdateProfileDto
    {
        public string UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
    }
}
