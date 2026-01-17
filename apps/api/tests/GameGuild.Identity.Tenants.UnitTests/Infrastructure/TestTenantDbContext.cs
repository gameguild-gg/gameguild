using System.Text.Json;
using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Moq;

namespace GameGuild.Identity.Tenants.UnitTests.Infrastructure;

public class TestTenantDbContext(DbContextOptions<TestTenantDbContext> options)
    : DbContext(options), IApplicationDbContext
{
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
