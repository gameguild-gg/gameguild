using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace GameGuild.Database;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext> {
  public ApplicationDbContext CreateDbContext(string[] args) {
    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

    // Load environment variables from .env file
    Env.Load();

    // Get PostgreSQL connection string from environment variables or build it from components
    var connectionString = GetPostgreSqlConnectionString();

    // Configure PostgreSQL only - no SQLite fallback
    optionsBuilder.UseNpgsql(connectionString);

    return new ApplicationDbContext(optionsBuilder.Options);
  }

  private static string GetPostgreSqlConnectionString() {
    // Try environment variable first
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

    // If not found, try to build from individual components (for Docker)
    if (string.IsNullOrEmpty(connectionString)) {
      var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
      var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
      var database = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "postgres";
      var username = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "postgres";
      var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";

      connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
    }

    if (string.IsNullOrEmpty(connectionString)) {
      throw new InvalidOperationException(
        "PostgreSQL database connection string not found. Please set DB_CONNECTION_STRING environment variable " +
        "or configure individual DB_HOST, DB_PORT, DB_DATABASE, DB_USERNAME, DB_PASSWORD environment variables. " +
        "SQLite is not supported - PostgreSQL only."
      );
    }

    return connectionString;
  }
}
