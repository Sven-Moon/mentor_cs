using MentoringApp.Api.Enums;

namespace MentoringApp.Api.Models
{
	public class Skill
	{
		public int Id { get; set; }

		// Properties
		public required string Name { get; set; } // e.g., "C#", "JavaScript", "Project Management"
		public required string NormalizedName { get; set; }
		public required string Description { get; set; } // Optional description of the skill
		public SkillStatus Status { get; set; } = SkillStatus.Pending; // Default to Pending
		public int? DuplicateOfSkillId { get; set; } //Can this not be done programatically outside of the model?

		// Foreign key relationship for duplicate skill reference
		public Skill? DuplicateOfSkill { get; set; }

		// Navigation properties for many-to-many relationships
		public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();		
		public ICollection<SkillCategory> Categories { get; set; } = new List<SkillCategory>();
		public ICollection<Mentorship> Mentorships { get; set; } = new List<Mentorship>();
		public ICollection<Tag> Tags { get; set; } = new List<Tag>();
	}
}
