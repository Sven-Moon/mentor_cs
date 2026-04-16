namespace MentoringApp.Api.DTOs.Mentorship
{
	public sealed class MilestoneDto
	{
		public int Id { get; set; }
		public int MentorshipId { get; set; }
		public string Title { get; set; } = default!;
		public string? Description { get; set; }
		public DateTime? DueDate { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? CompletedAt { get; set; }
	}

	public sealed class CreateMilestoneDto
	{
		public required string Title { get; set; }
		public string? Description { get; set; }
		public DateTime? DueDate { get; set; }
	}

	public sealed class UpdateMilestoneDto
	{
		public required string Title { get; set; }
		public string? Description { get; set; }
		public DateTime? DueDate { get; set; }
		public DateTime? CompletedAt { get; set; }
	}
}
