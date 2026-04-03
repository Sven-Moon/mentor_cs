namespace MentoringApp.Api.DTOs.Skills
{
	public class SkillCategoryDto
	{
		public int Id { get; set; }
		public required string Name { get; set; }
		public string? Description { get; set; }
	}
}
