namespace MentoringApp.Api.DTOs.Mentorship
{
	public sealed class NoteDto
	{
		public int Id { get; set; }
		public int MentorshipId { get; set; }
		public string AuthorId { get; set; } = default!;
		public string Content { get; set; } = default!;
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public sealed class CreateNoteDto
	{
		public required string Content { get; set; }
	}

	public sealed class UpdateNoteDto
	{
		public required string Content { get; set; }
	}
}
