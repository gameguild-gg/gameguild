using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace GameGuild.Database;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext> {
  public ApplicationDbContext CreateDbContext(string[] args) {
    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

    // Load environment variables from .env file
    Env.Load();

    // Get connection string from environment variable
    var connectionString =
      Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ??
      "Host=localhost;Port=5432;Database=gameguild;Username=postgres;Password=postgres123"; // Default to PostgreSQL

    // Always use PostgreSQL for migrations
    optionsBuilder.UseNpgsql(connectionString);

    return new ApplicationDbContext(optionsBuilder.Options);
  }
}
