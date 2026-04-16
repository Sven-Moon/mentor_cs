using MentoringApp.Api.Identity;

namespace MentoringApp.Api.Models
{
	public class Note
	{
		public int Id { get; set; }
		public int MentorshipId { get; set; }
		public string AuthorId { get; set; } = default!;
		public string Content { get; set; } = default!;
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

		// Navigation properties
		public Mentorship Mentorship { get; set; } = default!;
		public ApplicationUser Author { get; set; } = default!;
	}
}
