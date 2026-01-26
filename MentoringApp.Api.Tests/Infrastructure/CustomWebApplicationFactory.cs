using MentoringApp.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace MentoringApp.Api.Tests.Infrastructure;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private SqliteConnection _connection = default!;

  /// <summary>
  ///  removes any current ApplicationDbContext registration
  ///  adds a new ApplicationDbContext using an in-memory Sqlite database
  ///  with authentication scheme for testing
  /// </summary>
  /// <param name="builder"></param>
  protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configure for Jwt-based authentication
        // (see Login_WithValidCredentials_ReturnsJwtToken & Login_ReturnsJwt_WithExpectedClaims)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "THIS_IS_A_TEST_KEY_FOR_JWT_TOKEN_GENER",
                ["Jwt:Issuer"] = "MentoringApp.Test",
                ["Jwt:Audience"] = "MentoringApp.TestAudience"
            });
        });

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // ❌ Never call BuildServiceProvider() inside ConfigureServices()
            // ✅ Let ASP.NET Core build the container
            // ✅ Touch the database only from tests or startup filters
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.ConfigureWarnings(w =>
                    w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                "Test", options => { });

            services.AddAuthorization();
        });


  }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
