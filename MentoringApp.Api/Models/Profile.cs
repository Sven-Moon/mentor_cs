using MentoringApp.Api.Identity;

namespace MentoringApp.Api.Models
{
    public class Profile
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        // These can be absent, mark nullable to match DB optionality.
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; } = default!; // Navigation property to ApplicationUser
    }
}
