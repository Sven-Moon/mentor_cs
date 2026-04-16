namespace MentoringApp.Api.DTOs.Mentorship
{
	public sealed class SessionDto
	{
		public int Id { get; set; }
		public int MentorshipId { get; set; }
		public string? Title { get; set; }
		public DateTime ScheduledAt { get; set; }
		public int? Duration { get; set; }
		public string? Notes { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public sealed class CreateSessionDto
	{
		public string? Title { get; set; }
		public DateTime ScheduledAt { get; set; }
		public int? Duration { get; set; }
		public string? Notes { get; set; }
	}

	public sealed class UpdateSessionDto
	{
		public string? Title { get; set; }
		public DateTime ScheduledAt { get; set; }
		public int? Duration { get; set; }
		public string? Notes { get; set; }
	}
}
