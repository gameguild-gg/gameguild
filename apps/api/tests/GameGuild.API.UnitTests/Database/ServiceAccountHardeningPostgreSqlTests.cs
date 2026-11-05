using FluentAssertions;
using GameGuild.API.Database.Migrations;
using GameGuild.Identity.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class ServiceAccountHardeningPostgreSqlTests
{
    [Fact]
    public void Upgrade_OnlyWidensThePinAndAddsANullableAutomaticDeadline()
    {
        var operations = BuildOperations(down: false).Operations;
        operations.Should().HaveCount(2);
        var pin = operations.OfType<AlterColumnOperation>().Should().ContainSingle().Which;
        pin.Table.Should().Be("service_accounts");
        pin.Schema.Should().Be("gameguild.authentication");
        pin.Name.Should().Be("certificate_thumbprint");
        pin.MaxLength.Should().Be(64);
        pin.OldColumn.MaxLength.Should().Be(40);
        var deadline = operations.OfType<AddColumnOperation>().Should().ContainSingle().Which;
        deadline.Name.Should().Be("automatic_lockout_ends_at");
        deadline.IsNullable.Should().BeTrue();
        deadline.DefaultValue.Should().BeNull();
    }

    [Fact]
    public async Task Upgrade_PreservesLegacyLocksAndPersistsSha256PinsAndAutomaticDeadlines()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateEmptyAsync("service_account_upgrade");
        var legacy = Account("legacy", new string('A', 40));
        legacy.IsLocked = true;
        legacy.LockedAt = SystemClock.UtcNow.AddDays(-2);
        legacy.FailedAuthenticationAttempts = 10;
        await CreateLegacySchema(database.ConnectionString, legacy);
        await using (var upgrade = Current(database.ConnectionString)) await Apply(upgrade, down: false);

        var automatic = Account("automatic");
        automatic.RecordFailedAuthentication(lockThreshold: 1, lockoutDuration: TimeSpan.FromMinutes(7));
        await using (var write = Current(database.ConnectionString))
        {
            write.Set<ServiceAccount>().Add(automatic);
            await write.SaveChangesAsync();
            await Service(write).BindCertificateAsync(automatic.Id, new string('b', 64), null);
        }

        await using var read = Current(database.ConnectionString);
        var oldRow = await read.Set<ServiceAccount>().SingleAsync(account => account.Id == legacy.Id);
        oldRow.IsLocked.Should().BeTrue();
        oldRow.AutomaticLockoutEndsAt.Should().BeNull();
        oldRow.CanAuthenticate.Should().BeFalse();
        oldRow.FailedAuthenticationAttempts.Should().Be(10);
        oldRow.CertificateThumbprint.Should().Be(new string('A', 40));
        var newRow = await read.Set<ServiceAccount>().SingleAsync(account => account.Id == automatic.Id);
        newRow.CertificateThumbprint.Should().Be(new string('B', 64));
        newRow.IsLocked.Should().BeTrue();
        newRow.AutomaticLockoutEndsAt.Should().BeCloseTo(automatic.AutomaticLockoutEndsAt!.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task DuplicatePin_UsesRealUniqueConstraintAndPreservesTheBindingBeforeRetry()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateEmptyAsync("service_account_pin_conflict");
        var first = Account("first", new string('A', 40));
        var second = Account("second", new string('B', 40));
        await CreateLegacySchema(database.ConnectionString, first, second);
        await using (var upgrade = Current(database.ConnectionString)) await Apply(upgrade, down: false);
        await using var write = Current(database.ConnectionString);
        var service = Service(write);

        var conflictingBind = () => service.BindCertificateAsync(second.Id, new string('A', 40), null);

        var failure = (await conflictingBind.Should().ThrowAsync<ServiceAccountCertificateBindingConflictException>()).Which;
        failure.InnerException.Should().BeOfType<DbUpdateException>().Which.InnerException.Should()
            .BeOfType<PostgresException>().Which.SqlState.Should().Be("23505");
        write.Set<ServiceAccount>().Local.Single(account => account.Id == second.Id).CertificateThumbprint.Should().Be(new string('B', 40));
        await using (var read = Current(database.ConnectionString))
            (await read.Set<ServiceAccount>().SingleAsync(account => account.Id == second.Id)).CertificateThumbprint.Should().Be(new string('B', 40));

        await service.BindCertificateAsync(second.Id, new string('C', 64), null);
        await using var retried = Current(database.ConnectionString);
        (await retried.Set<ServiceAccount>().SingleAsync(account => account.Id == second.Id)).CertificateThumbprint.Should().Be(new string('C', 64));
    }

    [Fact]
    public async Task Downgrade_CannotSilentlyTruncateSha256PinsOrLoseAutomaticDeadlines()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateEmptyAsync("service_account_pin_downgrade");
        await CreateLegacySchema(database.ConnectionString);
        await using var context = Current(database.ConnectionString);
        await Apply(context, down: false);
        var account = Account("sha256", new string('D', 64));
        account.RecordFailedAuthentication(lockThreshold: 1);
        context.Set<ServiceAccount>().Add(account);
        await context.SaveChangesAsync();

        var downgrade = () => Apply(context, down: true);

        (await downgrade.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("22001");
        await using var read = Current(database.ConnectionString);
        var persisted = await read.Set<ServiceAccount>().SingleAsync();
        persisted.CertificateThumbprint.Should().Be(new string('D', 64));
        persisted.AutomaticLockoutEndsAt.Should().NotBeNull();
    }

    private static ServiceAccount Account(string clientId, string? pin = null) => new()
    {
        Id = Guid.NewGuid(), ClientId = clientId, Name = "Migration test", ClientSecretHash = "test-digest",
        IsActive = true, CertificateThumbprint = pin
    };

    private static ServiceAccountService Service(CurrentContext context) =>
        new(new ServiceAccountRepository(context), Mock.Of<IRefreshTokenHasher>(), NullLogger<ServiceAccountService>.Instance);

    private static CurrentContext Current(string connectionString) =>
        new(new DbContextOptionsBuilder<CurrentContext>().UseNpgsql(connectionString).Options);

    private static async Task CreateLegacySchema(string connectionString, params ServiceAccount[] accounts)
    {
        await using var legacy = new LegacyContext(new DbContextOptionsBuilder<LegacyContext>().UseNpgsql(connectionString).Options);
        await legacy.Database.EnsureCreatedAsync();
        legacy.Set<ServiceAccount>().AddRange(accounts);
        await legacy.SaveChangesAsync();
    }

    private static MigrationBuilder BuildOperations(bool down)
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var migration = new ExposedMigration();
        if (down) migration.BuildDown(builder);
        else migration.BuildUp(builder);
        return builder;
    }

    private static async Task Apply(CurrentContext context, bool down)
    {
        var commands = context.GetService<IMigrationsSqlGenerator>().Generate(BuildOperations(down).Operations, context.Model);
        await using var transaction = await context.Database.BeginTransactionAsync();
        foreach (var command in commands) await context.Database.ExecuteSqlRawAsync(command.CommandText);
        await transaction.CommitAsync();
    }

    private class CurrentContext(DbContextOptions options) : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.ApplyConfiguration(new ServiceAccountConfiguration());

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);
    }

    private sealed class LegacyContext(DbContextOptions<LegacyContext> options) : CurrentContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ServiceAccount>().Ignore(account => account.AutomaticLockoutEndsAt);
            modelBuilder.Entity<ServiceAccount>().Property(account => account.CertificateThumbprint).HasMaxLength(40);
        }
    }

    private sealed class ExposedMigration : HardenServiceAccountCertificatesAndLockout
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
        public void BuildDown(MigrationBuilder builder) => Down(builder);
    }
}
