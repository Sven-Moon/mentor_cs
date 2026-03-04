namespace MentoringApp.Api.Models
{
	public class SkillTag
	{
		public int SkillId { get; set; }
		public int TagId { get; set; }

		// Navigation properties
		public Skill Skill { get; set; } = default!;
		public Tag Tag { get; set; } = default!;
	}
}
