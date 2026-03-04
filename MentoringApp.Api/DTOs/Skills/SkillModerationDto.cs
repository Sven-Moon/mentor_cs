namespace MentoringApp.Api.DTOs.Skills
{
	public class SkillModerationDto
	{
		public required string Name { get; set; }
		public required string Description { get; set; }
		public List<string> Tags { get; set; } = new List<string>();
	}
}
