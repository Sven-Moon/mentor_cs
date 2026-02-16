namespace MentoringApp.Api.DTOs.Profiles
{
    public class ProfileDto
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
    }
}
