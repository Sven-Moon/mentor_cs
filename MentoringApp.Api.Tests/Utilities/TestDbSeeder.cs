using MentoringApp.Api.Data;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;
using Microsoft.AspNetCore.Identity;


namespace MentoringApp.Api.Tests.Utilities;


public static class TestDbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {

        var roles = new[] { "User", "Admin" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Users (Identity only)
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

        foreach (var user in users)
        {
            if (await userManager.FindByIdAsync(user.Id) == null)
            {
                await userManager.CreateAsync(user);
            }
        }

        // Assign roles
        await userManager.AddToRoleAsync(users[0], "User");
        await userManager.AddToRoleAsync(users[1], "Admin");

        db.ChangeTracker.Clear();

        // Profiles (EF)
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
                MentorId = "user-1",
                MenteeId = "user-1",
                Scope = "test scope",
                Status = "Active",
            },
            new Mentorship
            {
                MentorId = "user-2",
                MenteeId = "user-2",
                Scope = "test scope",
                Status = "Pending",
            }
        };

        db.Profiles.AddRange(profiles); 
        db.Mentorships.AddRange(mentorships);


        db.SaveChanges();

    }
}