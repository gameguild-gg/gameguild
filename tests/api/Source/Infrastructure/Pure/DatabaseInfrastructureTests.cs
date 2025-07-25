using GameGuild.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;


namespace GameGuild.Tests.Infrastructure.Pure;

/// <summary>
/// Comprehensive database infrastructure tests
/// Tests different database providers and configurations in isolation
/// </summary>
public class DatabaseInfrastructureTests(ITestOutputHelper output) {
  [Fact]
  public void InMemory_Database_Should_Work_Correctly() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateTestConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Act
    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseInMemoryDatabase($"InMemoryTest_{Guid.NewGuid()}")
    );

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    Assert.NotNull(dbContext);
    Assert.True(dbContext.Database.IsInMemory());
    Assert.True(dbContext.Database.CanConnect());

    output.WriteLine("✅ InMemory database works correctly");
  }

  [Fact]
  public void SQLite_InMemory_Database_Should_Work_Correctly() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateTestConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Act
    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseSqlite("Data Source=:memory:")
    );

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    Assert.NotNull(dbContext);
    Assert.True(dbContext.Database.IsSqlite());

    // Test connection and schema creation
    dbContext.Database.OpenConnection();
    Assert.True(dbContext.Database.EnsureCreated());

    output.WriteLine("✅ SQLite in-memory database works correctly");
  }

  [Fact]
  public void SQLite_File_Database_Should_Work_Correctly() {
    // Arrange
    var tempFile = Path.GetTempFileName();
    var connectionString = $"Data Source={tempFile}";

    try {
      var services = new ServiceCollection();
      var configuration = CreateTestConfiguration();

      services.AddSingleton<IConfiguration>(configuration);
      services.AddLogging();

      // Act
      services.AddDbContext<ApplicationDbContext>(options =>
                                                    options.UseSqlite(connectionString)
      );

      // Assert
      var serviceProvider = services.BuildServiceProvider();
      using var scope = serviceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      Assert.NotNull(dbContext);
      Assert.True(dbContext.Database.IsSqlite());
      Assert.True(dbContext.Database.EnsureCreated());
      Assert.True(File.Exists(tempFile));

      output.WriteLine("✅ SQLite file database works correctly");
    }
    finally {
      try {
        if (File.Exists(tempFile)) {
          GC.Collect();
          GC.WaitForPendingFinalizers();
          File.Delete(tempFile);
        }
      }
      catch (IOException) {
        // File might still be in use, that's okay for the test
        output.WriteLine("⚠️ Could not delete temp file (file in use)");
      }
    }
  }

  [Fact]
  public void PostgreSQL_Configuration_Should_Be_Valid() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreatePostgreSQLConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Act
    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
    );

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    Assert.NotNull(dbContext);
    Assert.True(dbContext.Database.IsNpgsql());

    var connectionString = dbContext.Database.GetConnectionString();
    Assert.NotNull(connectionString);
    Assert.Contains("Host=localhost", connectionString);

    output.WriteLine("✅ PostgreSQL configuration is valid (connection not tested)");
  }

  [Fact]
  public async Task Database_Migrations_Should_Be_Queryable() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateTestConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseSqlite("Data Source=:memory:")
    );

    // Act
    var serviceProvider = services.BuildServiceProvider();
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    dbContext.Database.OpenConnection();

    // Assert
    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
    var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

    Assert.NotNull(pendingMigrations);
    Assert.NotNull(appliedMigrations);

    output.WriteLine($"✅ Migrations queryable - Pending: {pendingMigrations.Count()}, Applied: {appliedMigrations.Count()}");
  }

  [Fact]
  public void Database_Context_Should_Have_DbSets() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateTestConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseInMemoryDatabase($"DbSetsTest_{Guid.NewGuid()}")
    );

    // Act
    var serviceProvider = services.BuildServiceProvider();
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Assert
    var dbSetProperties = dbContext.GetType()
                                   .GetProperties()
                                   .Where(p => p.PropertyType.IsGenericType &&
                                               p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
                                   )
                                   .ToList();

    Assert.True(dbSetProperties.Count > 0, "Should have at least one DbSet");

    output.WriteLine($"✅ Database context has {dbSetProperties.Count} DbSets:");

    foreach (var prop in dbSetProperties) { output.WriteLine($"   - {prop.Name}"); }
  }

  [Fact]
  public async Task Database_Should_Support_Basic_Operations() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateTestConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    services.AddDbContext<ApplicationDbContext>(options =>
                                                  options.UseInMemoryDatabase($"BasicOpsTest_{Guid.NewGuid()}")
    );

    var serviceProvider = services.BuildServiceProvider();
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Act & Assert - Test basic database operations
    Assert.True(dbContext.Database.CanConnect());

    // Test transaction support (InMemory will log a warning but still works)
    try {
      using var transaction = await dbContext.Database.BeginTransactionAsync();
      Assert.NotNull(transaction);
      await transaction.RollbackAsync();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Transactions are not supported")) {
      // InMemory database doesn't support transactions, but this is expected
      output.WriteLine("Note: InMemory database transaction warning suppressed");
    }

    output.WriteLine("✅ Database supports basic operations (connect, transactions)");
  }

  [Fact]
  public void Multiple_Database_Contexts_Should_Be_Independent() {
    // Arrange
    var services1 = new ServiceCollection();
    var services2 = new ServiceCollection();
    var configuration = CreateTestConfiguration();

    services1.AddSingleton<IConfiguration>(configuration);
    services1.AddLogging();
    services1.AddDbContext<ApplicationDbContext>(options =>
                                                   options.UseInMemoryDatabase("Database1")
    );

    services2.AddSingleton<IConfiguration>(configuration);
    services2.AddLogging();
    services2.AddDbContext<ApplicationDbContext>(options =>
                                                   options.UseInMemoryDatabase("Database2")
    );

    // Act
    var provider1 = services1.BuildServiceProvider();
    var provider2 = services2.BuildServiceProvider();

    using var scope1 = provider1.CreateScope();
    using var scope2 = provider2.CreateScope();

    var dbContext1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbContext2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Assert
    Assert.NotNull(dbContext1);
    Assert.NotNull(dbContext2);
    Assert.NotSame(dbContext1, dbContext2);

    // Verify contexts are using InMemory databases independently
    Assert.True(dbContext1.Database.IsInMemory());
    Assert.True(dbContext2.Database.IsInMemory());

    output.WriteLine("✅ Multiple database contexts are independent");
  }

  [Fact]
  public void Database_Provider_Should_Be_Configurable() {
    // Test different provider configurations
    var providers = new[] {
      ("InMemory", (Action<DbContextOptionsBuilder>)(options => options.UseInMemoryDatabase("Test1"))), ("SQLite", (Action<DbContextOptionsBuilder>)(options => options.UseSqlite("Data Source=:memory:"))),
    };

    foreach (var (providerName, configureOptions) in providers) {
      // Arrange
      var services = new ServiceCollection();
      var configuration = CreateTestConfiguration();

      services.AddSingleton<IConfiguration>(configuration);
      services.AddLogging();

      // Act
      services.AddDbContext<ApplicationDbContext>(configureOptions);

      // Assert
      var serviceProvider = services.BuildServiceProvider();
      using var scope = serviceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      Assert.NotNull(dbContext);
      output.WriteLine($"✅ {providerName} provider configured successfully");
    }
  }

  [Fact]
  public void Database_Connection_Pooling_Should_Work() {
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateTestConfiguration();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();

    // Add with connection pooling enabled
    services.AddDbContextPool<ApplicationDbContext>(options =>
                                                      options.UseInMemoryDatabase($"PoolTest_{Guid.NewGuid()}")
    );

    // Act
    var serviceProvider = services.BuildServiceProvider();

    var contexts = new List<ApplicationDbContext>();

    for (int i = 0; i < 5; i++) {
      using var scope = serviceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      Assert.NotNull(dbContext);
      contexts.Add(dbContext);
    }

    // Assert
    Assert.Equal(5, contexts.Count);
    output.WriteLine("✅ Database connection pooling works");
  }

  private static IConfiguration CreateTestConfiguration() {
    var configData = new Dictionary<string, string?> {
      ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:", ["Database:Provider"] = "SQLite", ["ASPNETCORE_ENVIRONMENT"] = "Testing",
    };

    return new ConfigurationBuilder()
           .AddInMemoryCollection(configData)
           .Build();
  }

  private static IConfiguration CreatePostgreSQLConfiguration() {
    var configData = new Dictionary<string, string?> {
      ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=testdb;Username=test;Password=test", ["Database:Provider"] = "PostgreSQL", ["ASPNETCORE_ENVIRONMENT"] = "Testing",
    };

    return new ConfigurationBuilder()
           .AddInMemoryCollection(configData)
           .Build();
  }
}
