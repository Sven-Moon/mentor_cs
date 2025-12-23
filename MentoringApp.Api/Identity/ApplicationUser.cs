using MentoringApp.Api.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace MentoringApp.Api.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public Profile Profile { get; set; } = new Profile(); // 1:1 relationship with Profile
        public bool IsActive { get; set; } = true; // Indicates if the user is active

        #region Collections
        public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>(); // Many-to-many relationship with Skill
        public ICollection<Mentorship> MentorshipsAsMentor { get; set; } = new List<Mentorship>(); // Mentorships where this user is a mentor
        public ICollection<Mentorship> MentorshipAsMentee { get; set; } = new List<Mentorship>(); // Mentorships where this user is a mentee

        public ICollection<Testimonial> ReceivedTestimonials { get; set; } = new List<Testimonial>(); // Testimonials received by this user
        public ICollection<Testimonial> WrittenTestimonials { get; set; } = new List<Testimonial>(); // Testimonials given by this user
        #endregion collections
    }

}