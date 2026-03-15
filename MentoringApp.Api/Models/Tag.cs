namespace MentoringApp.Api.Models
{
	public class Tag
	{
		public int Id { get; set; }

		// Properties
		public required string Name { get; set; } // e.g., "backend", "frontend", "devops", "soft skills"

		// Navigation properties for many-to-many relationships
		public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
	}
}
