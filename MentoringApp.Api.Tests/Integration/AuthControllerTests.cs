using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using MentoringApp.Api.DTOs;
using MentoringApp.Api.Identity;
using MentoringApp.Api.Data;
using MentoringApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MentoringApp.Api.Tests.Integration;

public class AuthControllerTests
    : IClassFixture<CustomWebApplicationFactory>
{
  private readonly HttpClient _client;
  private readonly CustomWebApplicationFactory _factory;

  public AuthControllerTests(CustomWebApplicationFactory factory)
  {
    _factory = factory;
    _client = factory.CreateClient();

    using var scope = factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
  }

  #region Register

  [Fact]
  public async Task Register_WithValidCredentials_ReturnsOk()
  {
    // Arrange
    var dto = new LoginRequestDto
    {
      Email = "newuser@test.com",
      Password = "StrongPassword123!"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("Registration successful", content);
  }

  [Fact]
  public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
  {
    // Arrange
    var dto = new LoginRequestDto
    {
      Email = "duplicate@test.com",
      Password = "StrongPassword123!"
    };

    await _client.PostAsJsonAsync("/api/auth/register", dto);

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Register_WithWeakPassword_ReturnsBadRequest()
  {
    // Arrange
    var dto = new LoginRequestDto
    {
      Email = "weakpass@test.com",
      Password = "123"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  #endregion

  #region Login

  [Fact]
  public async Task Login_WithValidCredentials_ReturnsJwtToken()
  {
    // Arrange
    var email = "loginuser@test.com";
    var password = "StrongPassword123!";

    await _client.PostAsJsonAsync("/api/auth/register",
        new LoginRequestDto
        {
          Email = email,
          Password = password
        });

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login",
        new LoginRequestDto
        {
          Email = email,
          Password = password
        });

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
    Assert.NotNull(authResponse);
    Assert.False(string.IsNullOrWhiteSpace(authResponse!.Token));
    Assert.Equal(email, authResponse.Email);
  }

  [Fact]
  public async Task Login_WithWrongPassword_ReturnsUnauthorized()
  {
    // Arrange
    var email = "wrongpass@test.com";

    await _client.PostAsJsonAsync("/api/auth/register",
        new LoginRequestDto
        {
          Email = email,
          Password = "CorrectPassword123!"
        });

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login",
        new LoginRequestDto
        {
          Email = email,
          Password = "WrongPassword!"
        });

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
  {
    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login",
        new LoginRequestDto
        {
          Email = "doesnotexist@test.com",
          Password = "AnyPassword123!"
        });

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  #endregion

  #region JWT Validation

  [Fact]
  public async Task Login_ReturnsJwt_WithExpectedClaims()
  {
    // Arrange
    var email = "claims@test.com";
    var password = "StrongPassword123!";

    await _client.PostAsJsonAsync("/api/auth/register",
        new LoginRequestDto
        {
          Email = email,
          Password = password
        });

    var response = await _client.PostAsJsonAsync("/api/auth/login",
        new LoginRequestDto
        {
          Email = email,
          Password = password
        });

    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
    var handler = new JwtSecurityTokenHandler();

    // Act
    var jwt = handler.ReadJwtToken(authResponse!.Token);

    // Assert
    Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
    Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub);

    Assert.True(jwt.ValidTo > DateTime.UtcNow);
  }

  #endregion
}
