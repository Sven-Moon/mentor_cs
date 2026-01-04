using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MentoringApp.Api.Data;


namespace MentoringApp.Api.Tests.Infrastructure;


public class CustomWebApplicationFactory
: WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real DbContext
            var dbDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));


            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }


            // Optional: if NpgsqlDataSource or other Npgsql services were added, remove them too
            //var npgsqlDataSource = services.FirstOrDefault(d => d.ServiceType.FullName == "Npgsql.NpgsqlDataSource");
            //if (npgsqlDataSource != null)
            //    services.Remove(npgsqlDataSource);



            // Replace with SQLite in-memory
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite("DataSource=:memory:");
            });


            // Replace authentication
            services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            "Test", options => { });


            var sp = services.BuildServiceProvider();


            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.OpenConnection();
            db.Database.EnsureCreated();
        });
    }
}