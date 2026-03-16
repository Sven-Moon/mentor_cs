using MentoringApp.Api.Enums;

namespace MentoringApp.Api.DTOs.UserSkills
{
	public class UserSkillDto
	{
		public int Id { get; set; }
		public string UserId { get; set; } = default!;
		public int SkillId { get; set; }
		public string SkillName { get; set; } = default!;
		public SkillLevel Level { get; set; }
		public int? YearsExperience { get; set; }
	}
}
