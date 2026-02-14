namespace MentoringApp.Api.DTOs.Profiles
{
    public class EditProfileDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string Bio { get; set; } = default!;
        public string Location { get; set; } = default!;
    }
}
