using MentoringApp.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
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
