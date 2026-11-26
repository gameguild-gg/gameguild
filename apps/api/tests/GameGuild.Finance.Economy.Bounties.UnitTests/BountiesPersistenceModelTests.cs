using FluentAssertions;
using GameGuild.Finance.Economy.Bounties.Persistence;
using GameGuild.Finance.Economy.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GameGuild.Finance.Economy.Bounties.UnitTests;

public sealed class BountiesPersistenceModelTests
{
    [Fact]
    public void Configure_MapsEscrowAndExactlyOneTerminalOutcome()
    {
        using var context = new ModelContext();
        var entities = context.GetService<IDesignTimeModel>().Model.GetEntityTypes().ToArray();

        entities.Select(entity => entity.GetTableName()).Should().BeEquivalentTo(
            "economy_bounties",
            "economy_bounty_escrow_fragments",
            "economy_events",
            "economy_bounty_terminal_events");

        Entity(entities, "economy_bounties").GetIndexes()
            .Should().Contain(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual(new[] { "IdempotencyKey" }));
        Entity(entities, "economy_bounty_terminal_events").GetIndexes()
            .Should().Contain(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual(new[] { "BountyId" }));
        // #1092 Tier 2.5: bounty expirations are rows of the unified event ledger,
        // one per bounty; this module re-asserts it and binds each one to its
        // bounty. The jsonb facet must be re-asserted too: contexts built from
        // this configuration alone run against the migrated template schema,
        // where the column is jsonb.
        var ledger = Entity(entities, "economy_events");
        ledger.FindProperty("EvidenceHashes")!.GetColumnType().Should().Be("jsonb");
        ledger.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual(new[] { "BountyId" }) &&
            foreignKey.PrincipalEntityType.GetTableName() == "economy_bounties" &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        ledger.GetIndexes().Should().Contain(index => index.IsUnique && index.GetFilter() == "\"StreamKind\" = 12" &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { "BountyId" }));
        Entity(entities, "economy_bounty_escrow_fragments").GetCheckConstraints()
            .Should().Contain(constraint => constraint.Name == "ck_economy_bounty_escrow_fragments_amount_positive");
        Entity(entities, "economy_bounty_escrow_fragments").FindProperty("Provenance")!
            .ClrType.Should().Be(typeof(ProvenanceKind));
    }

    [Fact]
    public void PersistenceRows_RoundTripEveryMappedProperty()
    {
        using var context = new ModelContext();
        var rows = context.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Select(entity => Activator.CreateInstance(entity.ClrType, nonPublic: true)!)
            .ToArray();
        AssertPropertyRoundTrips(rows);
    }

    private static void AssertPropertyRoundTrips(params object[] rows)
    {
        foreach (var row in rows)
        foreach (var property in row.GetType().GetProperties())
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            object value = type == typeof(string)
                ? "value"
                : type == typeof(Guid)
                    ? Guid.Parse("10000000-0000-0000-0000-000000000001")
                    : type == typeof(DateTimeOffset)
                        ? new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero)
                        : type == typeof(long)
                            ? 1L
                            : type == typeof(int)
                                ? 1
                                : type == typeof(bool)
                                    ? true
                                    : Enum.ToObject(type, 1);
            property.SetValue(row, value);
            property.GetValue(row).Should().Be(value);
        }
    }

    private static IReadOnlyEntityType Entity(IEnumerable<IReadOnlyEntityType> entities, string table) =>
        entities.Single(entity => entity.GetTableName() == table);

    private sealed class ModelContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseNpgsql("Host=localhost;Database=model;Username=model;Password=model");

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            new BountiesModelConfiguration().Configure(modelBuilder);
    }
}
