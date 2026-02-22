using MentoringApp.Api.Data;
using MentoringApp.Api.DTOs.Profiles;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Models;
using MentoringApp.Api.Tests.Infrastructure;

namespace MentoringApp.Api.Tests.Integration;

public class ProfileControllerTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private const string TestUserId = TestAuthHandler.TestUserId;

    public ProfileControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    // ---------- GET /api/profile/me ----------

    [Fact]
    public async Task GetMyProfile_WhenProfileDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyProfile_WhenProfileExists_ReturnsProfile()
    {
        SeedProfile();

        var response = await _client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal(TestUserId, profile.UserId);
        Assert.Equal("John", profile.FirstName);
    }

    // ---------- PUT /api/profile/me ----------

    [Fact]
    public async Task UpdateMyProfile_WhenProfileDoesNotExist_ReturnsNotFound()
    {
        var dto = new UpdateProfileDto
        {
            FirstName = "Updated",
            LastName = "User",
            Bio = "Updated Bio",
            Location = "TX"
        };

        var response = await _client.PutAsJsonAsync("/api/profile/me", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- helpers ----------

    private void SeedProfile()
    {
        SeedTestUser();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Profiles.Add(new Profile
        {
            UserId = TestUserId,
            FirstName = "John",
            LastName = "Doe",
            Bio = "Bio",
            Location = "NY",
            CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();
    }

    private void SeedTestUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!db.Users.Any(u => u.Id == TestAuthHandler.TestUserId))
        {
            db.Users.Add(new ApplicationUser
            {
                Id = TestAuthHandler.TestUserId,
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                NormalizedUserName = "TESTUSER@EXAMPLE.COM",
                NormalizedEmail = "TESTUSER@EXAMPLE.COM"
            });

            db.SaveChanges();
        }
    }
}
