namespace MentoringApp.Api.DTOs.Profiles
{
    public class ProfileDto
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Bio { get; set; }
        public required string Location { get; set; }
    }
}
