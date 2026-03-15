using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;
using MentoringApp.Api.Enums;

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

			if (context.Database.IsNpgsql())
			{
				await context.Database.MigrateAsync();
			}
			else
			{
				context.Database.EnsureCreated();
			}

			await SeedRoles(roleManager);
			var adminUser = await SeedAdminUser(userManager);
			var regularUser = await SeedRegularUser(userManager);
			await SeedSkillCategories(context);
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

			var categories = await context.SkillCategories.ToDictionaryAsync(c => c.Name);

			var csharp = new Skill
			{
				Name = "C#",
				Description = "A versatile programming language used for backend development, game development, and more.",
				Status = SkillStatus.Approved,
				Categories = new List<SkillCategory> { categories["Backend"] }
			};
			var javascript = new Skill
			{
				Name = "JavaScript",
				Description = "A popular programming language primarily used for frontend development to create interactive web pages.",
				Status = SkillStatus.Approved,
				Categories = new List<SkillCategory> { categories["Frontend"] }
			};
			var sql = new Skill
			{
				Name = "SQL",
				Description = "A domain-specific language used for managing and querying relational databases.",
				Status = SkillStatus.Approved,
				Categories = new List<SkillCategory> { categories["Databases"] }
			};
			var communication = new Skill
			{
				Name = "Communication",
				Description = "The ability to convey information effectively and efficiently.",
				Status = SkillStatus.Approved,
				Categories = new List<SkillCategory> { categories["Soft Skills"] }
			};

			context.Skills.AddRange(
				csharp,
				javascript,
				sql,
				communication);

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

		private static async Task SeedSkillCategories(ApplicationDbContext context)
		{
			if (context.SkillCategories.Any()) return;

			var categories = new List<SkillCategory>
			{
				new SkillCategory { Name = "Backend", Description = "Server-side development" },
				new SkillCategory { Name = "Frontend", Description = "User interface development" },
				new SkillCategory { Name = "DevOps", Description = "Infrastructure and deployment" },
				new SkillCategory { Name = "Databases", Description = "Database design and querying" },
				new SkillCategory { Name = "Architecture", Description = "System design and architecture" },
				new SkillCategory { Name = "Testing", Description = "Unit and integration testing" },
				new SkillCategory { Name = "Security", Description = "Application security" },
				new SkillCategory { Name = "Mobile", Description = "Mobile development" },
				new SkillCategory { Name = "Soft Skills", Description = "Communication and leadership" }
			};

			context.SkillCategories.AddRange(categories);
			await context.SaveChangesAsync();
		}
	}
}
