namespace MentoringApp.Api.DTOs.Profiles
{
    public class UpdateProfileDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Bio { get; set; }
        public required string Location { get; set; }
    }
}
