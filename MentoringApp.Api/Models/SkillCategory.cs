namespace MentoringApp.Api.Models
{
	public class SkillCategory
	{
		public int Id { get; set; }
		public required string Name { get; set; }
		public string ? Description { get; set; }

		public ICollection<Skill> Skills { get; set; } = new List<Skill>();
	}
}
