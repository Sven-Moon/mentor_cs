using MentoringApp.Api.Identity;

namespace MentoringApp.Api.Models
{
    public class Profile
    {
        public int Id { set; get; }
        public required string UserId { set; get; }         // Identity user FK
        public required string FirstName { set; get; }
        public required string LastName { set; get; }
        public required string Bio { set; get; }
        public required string Location { set; get; }
        public required DateTime CreatedAt { set; get; }

        public ApplicationUser User { get; set; } = default!; // Navigation property to ApplicationUser
    }
}