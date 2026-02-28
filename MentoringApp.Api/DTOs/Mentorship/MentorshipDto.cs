namespace MentoringApp.Api.DTOs.Mentorship
{
    public sealed class MentorshipDto
    {
        public int Id { get; set; }
        public string MentorId { get; set; } = default!;
        public string MenteeId { get; set; } = default!;
        public string Scope { get; set; } = default!;
        public string Status { get; set; } = default!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TestimonialDto[] Testimonials { get; set; } = Array.Empty<TestimonialDto>();

        // Concurrency token (required for PUT)
        public byte[] Version { get; set; } = default!;
    }
}
