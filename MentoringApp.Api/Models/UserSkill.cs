using MentoringApp.Api.Identity;
using MentoringApp.Api.Enums;

namespace MentoringApp.Api.Models
{
	public class UserSkill
	{
		public int Id { get; set; }

		// Foreign keys
		public required string UserId { get; set; }
		public int SkillId { get; set; }

		// properties
		public SkillLevel Level { get; set; } // Skill level (1-5)
		public int? YearsExperience { get; set; } // Years of experience in this skill

		// Navigation properties
		public ApplicationUser User { get; set; } = default!;
		public Skill Skill { get; set; } = default!;

	}
}
