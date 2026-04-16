using MentoringApp.Api.Models;

namespace MentoringApp.Api.DTOs.Mentorship
{
	public sealed class GoalDto
	{
		public int Id { get; set; }
		public int MentorshipId { get; set; }
		public string Title { get; set; } = default!;
		public string? Description { get; set; }
		public string Status { get; set; } = default!;
		public DateTime? DueDate { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? CompletedAt { get; set; }
	}

	public sealed class CreateGoalDto
	{
		public required string Title { get; set; }
		public string? Description { get; set; }
		public DateTime? DueDate { get; set; }
	}

	public sealed class UpdateGoalDto
	{
		public required string Title { get; set; }
		public string? Description { get; set; }
		public GoalStatus? Status { get; set; }
		public DateTime? DueDate { get; set; }
		public DateTime? CompletedAt { get; set; }
	}
}
