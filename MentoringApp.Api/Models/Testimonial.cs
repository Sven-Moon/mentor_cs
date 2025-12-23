using MentoringApp.Api.Identity;

namespace MentoringApp.Api.Models
{
    public class Testimonial
    {
        public int Id { get; set; }
        // Foreign keys
        public required string RecipientId { get; set; } // to Profile
        public required string AuthorId { get; set; } // to Profile
        public required int MentorshipId { get; set; } // to Mentorship

        public string? Content { get; set; } // The testimonial content
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Timestamp of when the testimonial was created

        // Navigation properties
        public ApplicationUser Recipient { get; set; }
        public ApplicationUser Author { get; set; }
        public Mentorship Mentorship { get; set; }
    }
}
