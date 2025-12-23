namespace MentoringApp.Api.Models
{
    public class Skill
    {
        public int Id { get; set; }

        // Properties
        public required string Name { get; set; } // e.g., "C#", "JavaScript", "Project Management"
        public required string Description { get; set; } // Optional description of the skill

        // Navigation property for many-to-many relationship with Profile
        public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    }
}
