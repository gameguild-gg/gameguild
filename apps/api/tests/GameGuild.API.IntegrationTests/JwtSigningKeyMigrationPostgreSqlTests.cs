using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authentication;
using GameGuild.TestSupport.Finance.Economy;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class JwtSigningKeyMigrationPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task SigningAndValidationUseTheRegisteredRotationStore()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var rotation = scope.ServiceProvider.GetRequiredService<IKeyRotationService>();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        await rotation.InitializeAsync();
        var before = await tokens.GenerateServiceAccountTokenAsync(Guid.NewGuid().ToString(),
            "synthetic-client", "synthetic-service", new HashSet<string> { "read:status" }, null);
        var firstId = new JwtSecurityTokenHandler().ReadJwtToken(before.Token).Header.Kid;
        Assert.NotEqual("GameGuild-jwt-key", firstId);
        Assert.Contains(await rotation.GetValidationKeysAsync(), key => key.KeyId == firstId);

        var next = await rotation.RotateKeyAsync("synthetic-auth-test");
        var after = await tokens.GenerateServiceAccountTokenAsync(Guid.NewGuid().ToString(),
            "synthetic-client", "synthetic-service", new HashSet<string> { "read:status" }, null);
        Assert.Equal(next.KeyId, new JwtSecurityTokenHandler().ReadJwtToken(after.Token).Header.Kid);
        Assert.NotEqual(firstId, next.KeyId);
        Assert.NotNull(tokens.ValidateToken(before.Token).Identity);
        Assert.NotNull(tokens.ValidateToken(after.Token).Identity);
    }

    [Fact]
    public async Task NativeModelAndMigration_PersistRotationKeysAndSupportRollback()
    {
        using var hostScope = fixture.Factory.Services.CreateScope();
        _ = hostScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Model;
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("jwt_rotation_migration");
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(database.ConnectionString).Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var migrations = db.Database.GetMigrations().ToList();
        var migration = Assert.Single(migrations, name => name.EndsWith("_AddJwtSigningKeys", StringComparison.Ordinal));
        var predecessor = migrations[migrations.IndexOf(migration) - 1];
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migration);

        var key = JwtSigningKey.CreateNew(1, DateTime.UtcNow, TimeSpan.FromDays(1));
        key.Activate();
        db.Add(key);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var reloaded = await db.Set<JwtSigningKey>().SingleAsync(row => row.Id == key.Id);
        Assert.Equal(key.KeyMaterial, reloaded.KeyMaterial);
        Assert.True(reloaded.IsActive);
        Assert.Equal(key.KeyId, reloaded.KeyId);

        await migrator.MigrateAsync(predecessor);
        Assert.Null(await db.Database.SqlQueryRaw<string?>("""
            SELECT to_regclass('"gameguild.authentication".jwt_signing_keys')::text AS "Value"
            """).SingleAsync());
        await migrator.MigrateAsync(migration);
        Assert.Empty(await db.Set<JwtSigningKey>().AsNoTracking().ToListAsync());
        await migrator.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
