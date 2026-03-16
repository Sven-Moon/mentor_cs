using MentoringApp.Api.Enums;

namespace MentoringApp.Api.DTOs.Skills
{
	public class UserSkillDto
	{
		public int Id { get; set; }

		// Foreign keys
		public required string UserId { get; set; }
		public int SkillId { get; set; }

		// properties
		public SkillLevel Level { get; set; } // Skill level (1-5)

		// Navigation properties
		public List<UserSkillDto> Users { get; set; } = new();

	}
}
