using MentoringApp.Api.Data;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;


namespace MentoringApp.Api.Tests.Utilities;


public static class TestDbSeeder
{
    public static void Seed(ApplicationDbContext db)
    {
        var users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = "user-1",
                UserName = "user1@test.com",
                Email = "user1@test.com"
            },
            new ApplicationUser
            {
                Id = "user-2",
                UserName = "user2@test.com",
                Email = "user2@test.com"
            }
        };

        var profiles = new List<Profile>
        {
            new Profile
            {
                UserId = "user-1",
                FirstName = "User",
                LastName = "One",
                Bio = "Bio One",
                Location = "Location One",
                CreatedAt = DateTime.UtcNow
            },
            new Profile
            {
                UserId = "user-2",
                FirstName = "User",
                LastName = "Two",
                Bio = "Bio Two",
                Location = "Location Two",
                CreatedAt = DateTime.UtcNow
            }
        };

        var mentorships = new List<Mentorship>
        { 
            new Mentorship
            {
                MentorId = "mentor-1",
                MenteeId = "mentee-1",
                Scope = "test scope",
                Status = "Active"
            },
            new Mentorship
            {
                MentorId = "mentor-2",
                MenteeId = "mentee-2",
                Scope = "test scope",
                Status = "Pending"
            }
        };

        db.Users.AddRange(users);
        db.Profiles.AddRange(profiles); 
        db.Mentorships.AddRange(mentorships);


        db.SaveChanges();

    }
}