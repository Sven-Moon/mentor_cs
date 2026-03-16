using MentoringApp.Api.Enums;

namespace MentoringApp.Api.DTOs.UserSkills
{
	public class UpdateUserSkillDto
	{
		public int SkillId { get; set; }
		public required SkillLevel Level { get; set; }
		public int? YearsExperience { get; set; }
	}
}
