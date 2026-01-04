using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Api.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            await SeedRoles(roleManager);
            var adminUser = await SeedAdminUser(userManager);
            var regularUser = await SeedRegularUser(userManager);
            await SeedSkills(context);
            await SeedSampleProfiles(context, adminUser);
            await SeedSampleMentorships(context, adminUser, regularUser);
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task<ApplicationUser> SeedAdminUser(UserManager<ApplicationUser> userManager)
        {
            var adminEmail = "admin@mentoringapp.local";
            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(admin, "Admin!234");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            return admin;
        }

        private static async Task<ApplicationUser> SeedRegularUser(UserManager<ApplicationUser> userManager)
        {
            var email = "user@mentoringapp.local";
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "User!234");
                await userManager.AddToRoleAsync(user, "User");
            }

            return user;
        }

        private static async Task SeedSkills(ApplicationDbContext context)
        {
            if (context.Skills.Any()) return;

            var skills = new List<Skill>
            {
                new Skill { Name = "C#", Description = "C# is a modern, object-oriented programming language developed by Microsoft." },
                new Skill { Name = "JavaScript", Description = "JavaScript is a versatile programming language commonly used for web development." },
                new Skill { Name = "SQL", Description = "SQL (Structured Query Language) is used for managing and manipulating relational databases." },
                new Skill { Name = "Project Management", Description = "Project Management involves planning, executing, and closing projects." },
                new Skill { Name = "Communication", Description = "Communication skills are essential for effective collaboration and information exchange." }
            };

            context.Skills.AddRange(skills);
            await context.SaveChangesAsync();
        }

        private static async Task SeedSampleProfiles(ApplicationDbContext context, ApplicationUser adminUser)
        {
            if (context.Profiles.Any()) return;

            var profile = new Profile
            {
                UserId = adminUser.Id,
                FirstName = "Admin",
                LastName = "User",
                Bio = "Platform administrator",
                Location = "Loveland, CO",
                CreatedAt = DateTime.UtcNow
            };

            context.Profiles.Add(profile);
            await context.SaveChangesAsync();
        }

        private static async Task SeedSampleMentorships(ApplicationDbContext context, ApplicationUser adminUser, ApplicationUser user)
        {
            if (context.Mentorships.Any()) return;

            var mentorship = new Mentorship
            {
                MentorId = adminUser.Id,
                MenteeId = user.Id,
                Scope = "Intro mentorship",
                StartDate = DateTime.UtcNow,
                Status = "Active"
            };

            context.Mentorships.Add(mentorship);
            await context.SaveChangesAsync();
        }
    }
}