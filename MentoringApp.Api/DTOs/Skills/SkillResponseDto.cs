using MentoringApp.Api.Enums;

namespace MentoringApp.Api.DTOs.Skills
{
	public class SkillResponseDto
	{
		public int Id { get; set; }
		public required string Name { get; set; }
		public required string Description { get; set; }

		public SkillStatus Status { get; set; }
		public int? DuplicateOfSkillId { get; set; } = null;

		public List<string> Categories { get; set; } = new List<string>();
		public List<string> Tags { get; set; } = new List<string>();
	}
}
