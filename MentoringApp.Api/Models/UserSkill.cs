using MentoringApp.Api.Identity;

namespace MentoringApp.Api.Models
{
    public class UserSkill
    {
        public int Id { get; set; }

        // Foreign keys
        public required string  UserId { get; set; }
        public int SkillId { get; set; }

        // properties
        public int Level { get; set; } // Skill level (1-5)

        // Navigation properties
        public ApplicationUser User { get; set; } = default!;
        public Skill Skill { get; set; } = default!;

    }
}
