using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Social.Profiles.UnitTests;

internal sealed class SocialProfilesTestDbContext(DbContextOptions<SocialProfilesTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    private static readonly InMemoryDatabaseRoot DatabaseRoot = new();

    public int SaveChangesCallCount { get; private set; }

    public static SocialProfilesTestDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<SocialProfilesTestDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString(), DatabaseRoot)
            .Options;

        return new SocialProfilesTestDbContext(options);
    }

    public void ResetSaveChangesCallCount()
    {
        SaveChangesCallCount = 0;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return base.SaveChangesAsync(cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Transactions are not supported by the in-memory test context.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => new SocialProfilesModelConfiguration().Configure(modelBuilder);
}
