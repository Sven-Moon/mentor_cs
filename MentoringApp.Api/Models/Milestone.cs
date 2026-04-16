namespace MentoringApp.Api.Models
{
    public class Milestone
    {
        public int Id { get; set; }
        public int MentorshipId { get; set; }

        public required string Title { get; set; }
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation property
        public Mentorship Mentorship { get; set; } = default!;
    }
}
