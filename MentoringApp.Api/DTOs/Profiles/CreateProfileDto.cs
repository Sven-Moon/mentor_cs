namespace MentoringApp.Api.DTOs.Profiles
{
    public class UpsertProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Bio { get; set; } = default!;
        public string? Location { get; set; } = default!;
    }
}
