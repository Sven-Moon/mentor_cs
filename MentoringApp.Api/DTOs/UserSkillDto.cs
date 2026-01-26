using MentoringApp.Api.Identity;

namespace MentoringApp.Api.DTOs
{
    public class UserSkillDto
    {
        public int Id { get; set; }

        // Foreign keys
        public required string  UserId { get; set; }
        public int SkillId { get; set; }

        // properties
        public int Level { get; set; } // Skill level (1-5)

        // Navigation properties
        public List<UserSkillDto> Users { get; set; } = new();

    }
}
