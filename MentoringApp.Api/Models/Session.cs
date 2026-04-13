namespace MentoringApp.Api.Models
{
	public class Session
	{
		public int Id { get; set; }
		public int MentorshipId { get; set; }

		public string? Title { get; set; }
		public DateTime ScheduledAt { get; set; }
		public int? Duration { get; set; } // minutes
		public string? Notes { get; set; }

		public DateTime CreatedAt { get; set; }

		// Navigation property
		public Mentorship Mentorship { get; set; } = default!;
	}
}
