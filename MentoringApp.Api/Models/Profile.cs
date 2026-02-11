using MentoringApp.Api.Identity;

namespace MentoringApp.Api.Models
{
    public class Profile
    {
        public int Id { set; get; }
        public required string UserId { set; get; }         // Identity user FK
        public required string FirstName { set; get; }
        public required string LastName { set; get; }
        public string Bio { set; get; } = default!;
        public string Location { set; get; } = default!;
        public required DateTime CreatedAt { set; get; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; } = default!; // Navigation property to ApplicationUser
    }
}