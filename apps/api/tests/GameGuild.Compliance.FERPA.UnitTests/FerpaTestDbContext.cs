using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Compliance.FERPA.UnitTests;

internal sealed class FerpaTestDbContext(DbContextOptions<FerpaTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public static FerpaTestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<FerpaTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FerpaTestDbContext(options);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Transactions are not supported by the in-memory test context.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => new FerpaModelConfiguration().Configure(modelBuilder);
}
