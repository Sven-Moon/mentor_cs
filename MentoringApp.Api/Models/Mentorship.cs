using MentoringApp.Api.Identity;

namespace MentoringApp.Api.Models
{
    public class Mentorship
    {
        public int Id { get; set; }
        public required string MentorId { get; set; }
        public required string MenteeId { get; set; }
        public required string Scope { get; set; } // e.g., "Career Guidance", "Technical Skills", etc.

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public required string Status { get; set; } // e.g., "Active", "Completed", "Cancelled"

        // Navigation properties
        public ApplicationUser Mentor { get; set; } = default!;
        public ApplicationUser Mentee { get; set; } = default!;

        // Collection of testimonials related to this mentorship
        public ICollection<Testimonial> Testimonials { get; set; } = new List<Testimonial>();
    }
}