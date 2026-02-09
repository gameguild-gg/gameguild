using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Moq;

namespace GameGuild.Identity.Tenants.UnitTests.Infrastructure;

public class TestTenantDbContext(DbContextOptions<TestTenantDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<TenantMember> TenantMembers { get; set; } = null!;
    public DbSet<TenantDomain> TenantDomains { get; set; } = null!;
    public DbSet<TenantSettings> TenantSettings { get; set; } = null!;
    public DbSet<TenantMetadata> TenantMetadata { get; set; } = null!;
    public DbSet<UsageTracking> UsageTracking { get; set; } = null!;
    public DbSet<TenantAuditLog> TenantAuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var dictionaryConverter = new ValueConverter<Dictionary<string, object?>?, string?>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrWhiteSpace(v)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(v, (JsonSerializerOptions?)null));

        var dictionaryComparer = new ValueComparer<Dictionary<string, object?>?>(
            (left, right) => DictionaryEquals(left, right),
            value => value == null ? 0 : value.Aggregate(0, (acc, pair) => HashCode.Combine(acc, pair.Key, pair.Value)),
            value => value == null ? null : new Dictionary<string, object?>(value));

        var stringDictionaryConverter = new ValueConverter<Dictionary<string, string>?, string?>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrWhiteSpace(v)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null));

        var stringDictionaryComparer = new ValueComparer<Dictionary<string, string>?>(
            (left, right) => DictionaryEquals(left, right),
            value => value == null ? 0 : value.Aggregate(0, (acc, pair) => HashCode.Combine(acc, pair.Key, pair.Value)),
            value => value == null ? null : new Dictionary<string, string>(value));

        // TenantAuditLog configuration
        modelBuilder.Entity<TenantAuditLog>(builder =>
        {
            builder.Property(x => x.BeforeValues)
                .HasConversion(dictionaryConverter)
                .Metadata.SetValueComparer(dictionaryComparer);

            builder.Property(x => x.AfterValues)
                .HasConversion(dictionaryConverter)
                .Metadata.SetValueComparer(dictionaryComparer);

            builder.Property(x => x.Metadata)
                .HasConversion(stringDictionaryConverter)
                .Metadata.SetValueComparer(stringDictionaryComparer);
        });

        // Tenant configuration - minimal for testing
        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired();
            builder.Property(t => t.Slug).IsRequired();
        });

        // TenantMember configuration
        modelBuilder.Entity<TenantMember>(builder =>
        {
            builder.HasKey(tm => tm.Id);
            builder.Property(tm => tm.UserId).IsRequired();
            builder.Property(tm => tm.TenantId).IsRequired();
            builder.Property(tm => tm.Role).IsRequired();
            builder.HasIndex(tm => new { tm.UserId, tm.TenantId }).IsUnique();
        });

        // TenantDomain configuration
        modelBuilder.Entity<TenantDomain>(builder =>
        {
            builder.HasKey(td => td.Id);
            builder.Property(td => td.TenantId).IsRequired();
            builder.Property(td => td.TopLevelDomain).IsRequired();
        });

        // TenantSettings configuration
        modelBuilder.Entity<TenantSettings>(builder =>
        {
            builder.HasKey(ts => ts.Id);
            builder.Property(ts => ts.TenantId).IsRequired();
            builder.HasIndex(ts => ts.TenantId).IsUnique();
        });

        // TenantMetadata configuration
        modelBuilder.Entity<TenantMetadata>(builder =>
        {
            builder.HasKey(tm => tm.Id);
            builder.Property(tm => tm.TenantId).IsRequired();
            builder.HasIndex(tm => tm.TenantId).IsUnique();
        });

        // UsageTracking configuration
        modelBuilder.Entity<UsageTracking>(builder =>
        {
            builder.HasKey(ut => ut.Id);
            builder.Property(ut => ut.TenantId).IsRequired();
            builder.Property(ut => ut.ResourceType).IsRequired();
        });
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Mock.Of<IDbContextTransaction>());
    }

    private static bool DictionaryEquals<TKey, TValue>(IDictionary<TKey, TValue>? left, IDictionary<TKey, TValue>? right)
        where TKey : notnull
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return false;
        if (left.Count != right.Count) return false;
        return left.All(pair => right.TryGetValue(pair.Key, out var value) && Equals(value, pair.Value));
    }
}
