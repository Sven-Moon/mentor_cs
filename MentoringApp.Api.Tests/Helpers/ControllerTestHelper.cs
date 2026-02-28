using MentoringApp.Api.Controllers;
using MentoringApp.Api.Data;
using MentoringApp.Api.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;


namespace MentoringApp.Api.Tests.Helpers
{
    public class ControllerTestHelper
    {
        public static MentorshipsController Create(
                ApplicationDbContext context,
                UserManager<ApplicationUser> userManager,
                string userId,
                bool isAdmin = false)
        {
            var controller = new MentorshipsController(context, userManager);

            var claims = new List<Claim>
                        {
                                new Claim(ClaimTypes.NameIdentifier, userId)
                        };

            if (isAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            return controller;
        }
    }
}
