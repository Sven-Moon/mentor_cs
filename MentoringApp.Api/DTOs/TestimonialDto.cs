namespace MentoringApp.Api.DTOs
{
    public sealed class TestimonialDto
    {
        public int Id { get; set; }
        public required string RecipientId { get; set; }
        public string AuthorId { get; set; } = default!;
        public required int MentorshipId { get; set; }
        public string? Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
