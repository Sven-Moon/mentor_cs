namespace MentoringApp.Api.DTOs.Auth
{
    public class AdminUserDto
    {
        public required string Id { get; set; }          // IdentityUser Id
        public required string Email { get; set; }

        // Identity / system-level info
        public required bool EmailConfirmed { get; set; }
        public required bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }

        // Roles are system-level here (Admin, Moderator, User)
        public required IList<string> Roles { get; set; }

        // Audit / operational data
        //public DateTime CreatedAt { get; set; }
        //public DateTime? LastLoginAt { get; set; }
    }
}
