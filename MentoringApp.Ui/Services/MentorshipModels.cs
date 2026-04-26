namespace MentoringApp.Ui.Services;

/// <summary>Represents a mentorship as returned by GET /api/mentorships (entity projection).</summary>
public class MentorshipSummary
{
    public int Id { get; set; }
    public string MentorId { get; set; } = "";
    public string MenteeId { get; set; } = "";
    public MentorshipUserSummary? Mentor { get; set; }
    public MentorshipUserSummary? Mentee { get; set; }
    public string Scope { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? LastInteractionDate { get; set; }
}

public class MentorshipUserSummary
{
    public string Id { get; set; } = "";
    public string? UserName { get; set; }
}

public class UserSummary
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
}
