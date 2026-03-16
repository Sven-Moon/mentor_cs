using MentoringApp.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Api.Tests.Helpers
{
	public static class TestDbContextFactory
	{
		/// <summary>
		/// Creates an ApplicationDbContext backed by a shared SQLite in-memory connection.
		/// The caller is responsible for disposing both the context and the connection.
		/// Keep the connection open for the lifetime of the test; closing it destroys the database.
		/// </summary>
		public static (ApplicationDbContext Context, SqliteConnection Connection) Create()
		{
			var connection = new SqliteConnection("DataSource=:memory:");
			connection.Open();

			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseSqlite(connection)
				.Options;

			var context = new ApplicationDbContext(options);
			context.Database.EnsureCreated();

			return (context, connection);
		}
	}
}
