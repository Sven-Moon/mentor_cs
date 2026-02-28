namespace MentoringApp.Api.DTOs
{
    public class SkillDto
    {
        public int Id { get; set; }

        // Properties
        public required string Name { get; set; } // e.g., "C#", "JavaScript", "Project Management"
        public required string Description { get; set; } // Optional description of the skill
    }
}
