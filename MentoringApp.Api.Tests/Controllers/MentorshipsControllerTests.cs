using MentoringApp.Api.Tests.Helpers;
using MentoringApp.Api.Identity;

using MentoringApp.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MentoringApp.Api.Tests.Controllers;

public class MentorshipsControllerTests
{
    [Fact]
    public async Task GetMentorships_Admin_ReturnsAll()
    {
        /// <summary>
        /// Using real EF Core + SQLite
        /// Enforcing real foreign keys
        /// Testing authorization logic via claims, not Identity internals
        /// </summary>
        using var context = DbContextFactory.CreateContext();

        // Arrange users
        var mentor1 = new ApplicationUser { Id = "a", UserName = "mentorA" };
        var mentee1 = new ApplicationUser { Id = "b", UserName = "menteeB" };
        var mentor2 = new ApplicationUser { Id = "c", UserName = "mentorC" };
        var mentee2 = new ApplicationUser { Id = "d", UserName = "menteeD" };

        context.Users.AddRange(mentor1, mentee1, mentor2, mentee2);

        context.Mentorships.AddRange(
            new Mentorship { 
                MentorId = mentor1.Id, 
                MenteeId = mentee1.Id, 
                Scope = "Scope1", 
                Status = "Active",
                RowVersion = Guid.NewGuid().ToByteArray()
            },
            new Mentorship { 
                MentorId = mentor2.Id, 
                MenteeId = mentee2.Id, 
                Scope = "Scope2", 
                Status = "Inactive",
                RowVersion = Guid.NewGuid().ToByteArray()
            }
            );

        await context.SaveChangesAsync();

        var userManager = MockUserManager<ApplicationUser>.Create();

            var controller = ControllerTestHelper.Create(
                context,
                userManager.Object, // userManager: type error
                userId: "admin",
                isAdmin: true);

        var result = await controller.GetMentorships();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsAssignableFrom<IEnumerable<Mentorship>>(ok.Value);

        Assert.Equal(2, data.Count());
        Assert.Contains(data, m => m.MentorId == "a" && m.MenteeId == "b");
        Assert.Contains(data, m => m.MentorId == "c" && m.MenteeId == "d");
    }
}
