using MentoringApp.Api.Enums;

namespace MentoringApp.Api.DTOs.Skills
{
	public class AddUserSkillDto
	{
		// Foreign keys
		public required string UserId { get; set; }
		public int SkillId { get; set; }

		// properties
		public SkillLevel Level { get; set; } // Skill level (1-5)
		public int? YearsExperience { get; set; } // Years of experience in this skill
	}
}
