
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using GameGuild.CQRS;
using Moq;

namespace GameGuild.Resources.IntegrationTests.Infrastructure;

/// <summary>
/// Lightweight test-specific DbContext for Resource quota integration tests.
/// Contains only the entities needed for quota testing, avoiding complex dependencies.
/// </summary>
public class ResourceQuotaTestDbContext : DbContext, IApplicationDbContext
{
    public ResourceQuotaTestDbContext(DbContextOptions<ResourceQuotaTestDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Resource Quotas DbSet
    /// </summary>
    public DbSet<ResourceQuota> ResourceQuotas => Set<ResourceQuota>();

    /// <summary>
    /// Usage Records DbSet
    /// </summary>
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is EntityBase<Guid> &&
                entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(nameof(EntityBase<Guid>.Version)).CurrentValue =
                    (int)entry.Property(nameof(EntityBase<Guid>.Version)).CurrentValue! + 1;
            }
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries)
            {
                entry.State = EntityState.Detached;
            }

            throw;
        }
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Apply Resources module configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ResourceQuota).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Resources") == true &&
                    type.GetInterfaces().Any(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

        base.OnModelCreating(modelBuilder);
    }
}

public sealed class ResourceQuotaPostgreSqlScope : IAsyncDisposable
{
    private ResourceQuotaPostgreSqlScope(ResourceQuotaTestDbContext context)
    {
        Context = context;
        Repository = new ResourceQuotaRepository(context);

        var usageRepository = new UsageRecordRepository(context);
        var publisher = Mock.Of<IPublisher>();
        var management = new QuotaManagementService(
            Repository,
            usageRepository,
            publisher,
            NullLogger<QuotaManagementService>.Instance);
        var enforcement = new QuotaEnforcementService(
            Repository,
            management,
            publisher,
            NullLogger<QuotaEnforcementService>.Instance);
        var maintenance = new QuotaMaintenanceService(
            Repository,
            usageRepository,
            management,
            publisher,
            NullLogger<QuotaMaintenanceService>.Instance);

        Service = new ResourceQuotaService(management, enforcement, maintenance);
    }

    public ResourceQuotaTestDbContext Context { get; }

    public IResourceQuotaRepository Repository { get; }

    public IResourceQuotaService Service { get; }

    public static ResourceQuotaPostgreSqlScope Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ResourceQuotaTestDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ResourceQuotaPostgreSqlScope(new ResourceQuotaTestDbContext(options));
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}
