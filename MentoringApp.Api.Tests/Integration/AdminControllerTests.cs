using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Auth;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Tests.Infrastructure;
using MentoringApp.Api.Tests.Utilities;
using Microsoft.AspNetCore.Identity;
using System.Net.Http.Headers;


namespace MentoringApp.Api.Tests.Integration;


public class AdminControllerTests
: IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;


    public AdminControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure the database is created and seeded
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        TestDbSeeder.SeedAsync(db, userManager, roleManager)
                .GetAwaiter()
                .GetResult();
    }


    [Fact]
    public async Task GetAllMentorships_AsAdmin_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/mentorships");


        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var mentorships = await response.Content
        .ReadFromJsonAsync<List<object>>();


        Assert.NotNull(mentorships);
        Assert.NotEmpty(mentorships);
        Assert.True(mentorships.Count >= 2);
    }


    [Fact]
    public async Task GetAllUsers_ReturnsAllUsers()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/users");


        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);


        var users = await response.Content
        .ReadFromJsonAsync<List<AdminUserDto>>();


        Assert.NotNull(users);
        Assert.Equal(2, users.Count);


        Assert.All(users, user =>
        {
            Assert.NotNull(user.Email);
            Assert.NotEmpty(user.Roles);
        });
    }
}
