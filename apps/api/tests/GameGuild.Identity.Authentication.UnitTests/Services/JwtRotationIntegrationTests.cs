using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using GameGuild.Configuration.ApplicationLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class JwtRotationIntegrationTests
{
    private static JwtOptions Options() => new()
    {
        SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        Issuer = "rotation-test", Audience = "rotation-consumer", AccessTokenExpirationMinutes = 20
    };

    [Fact]
    public void AuthenticationRegistration_ProvidesTheRotationStoreAndWorker()
    {
        var services = new ServiceCollection();
        services.AddAuthenticationData(new ConfigurationBuilder().Build());
        Assert.Contains(services, service => service.ServiceType == typeof(IKeyRotationService)
            && service.ImplementationType == typeof(KeyRotationService)
            && service.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task Rotation_ChangesUserAndServiceAccountSigningKeysAndPreservesOldTokens()
    {
        await using var db = NewDatabase();
        var cache = new JwtSigningKeyCache();
        var rotation = Rotation(db, cache);
        await rotation.InitializeAsync();
        var service = Tokens(Options(), rotation, cache);
        var before = await UserToken(service);
        var first = new JwtSecurityTokenHandler().ReadJwtToken(before).Header.Kid;
        Assert.NotEqual("GameGuild-jwt-key", first);
        var next = await rotation.RotateKeyAsync();
        var after = await UserToken(service);
        var machine = await service.GenerateServiceAccountTokenAsync(Guid.NewGuid().ToString(),
            "client", "service", new HashSet<string> { "read:data" }, null);
        Assert.Equal(next.KeyId, new JwtSecurityTokenHandler().ReadJwtToken(after).Header.Kid);
        Assert.Equal(next.KeyId, new JwtSecurityTokenHandler().ReadJwtToken(machine.Token).Header.Kid);
        Assert.NotEqual(first, next.KeyId);
        Assert.True(await service.ValidateTokenAsync(before));
        Assert.True(await service.ValidateTokenAsync(after));
        Assert.True(await service.ValidateTokenAsync(machine.Token));
    }

    [Theory]
    [InlineData("async")]
    [InlineData("sync")]
    [InlineData("expired-principal")]
    public async Task OtherNodeRotation_IsVisibleWithoutLocalRotationOrRestart(string surface)
    {
        var options = Options();
        var dbOptions = new DbContextOptionsBuilder<KeyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var nodeA = new KeyDbContext(dbOptions);
        await using var nodeB = new KeyDbContext(dbOptions);
        var cacheA = new JwtSigningKeyCache();
        var cacheB = new JwtSigningKeyCache();
        var rotationA = Rotation(nodeA, cacheA);
        var rotationB = Rotation(nodeB, cacheB);
        await rotationA.InitializeAsync();
        await rotationB.InitializeAsync();
        var reader = Tokens(options, rotationB, cacheB);
        await rotationA.RotateKeyAsync();
        var encoded = await UserToken(Tokens(options, rotationA, cacheA));

        if (surface == "async") { Assert.True(await reader.ValidateTokenAsync(encoded)); }
        else if (surface == "sync") { Assert.NotNull(reader.ValidateToken(encoded).Identity); }
        else { Assert.NotNull(reader.GetPrincipalFromExpiredToken(encoded).Identity); }
    }

    [Fact]
    public async Task CachedKey_IsRejectedAfterItsValidityWindowEvenWithoutStoreRefresh()
    {
        var now = DateTime.UtcNow;
        await using var db = NewDatabase();
        var key = JwtSigningKey.CreateNew(1, now.AddMinutes(-1), TimeSpan.FromMinutes(2));
        key.Activate();
        db.Add(key);
        await db.SaveChangesAsync();
        var cache = new JwtSigningKeyCache();
        await Rotation(db, cache).InitializeAsync();
        var fallback = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)) { KeyId = "fallback" };
        Assert.Contains(cache.GetValidationKeys(fallback), candidate => candidate.KeyId == key.KeyId);
        SystemClock.SetProvider(new FixedTimeProvider(now.AddMinutes(2)));
        try { Assert.DoesNotContain(cache.GetValidationKeys(fallback), candidate => candidate.KeyId == key.KeyId); }
        finally { SystemClock.Reset(); }
    }

    [Fact]
    public async Task LateRotation_KeepsPredecessorValidForAlreadyIssuedTokenLifetime()
    {
        await using var db = NewDatabase();
        var now = DateTime.UtcNow;
        var old = JwtSigningKey.CreateNew(1, now.AddMinutes(-1), TimeSpan.FromMinutes(2));
        old.Activate();
        db.Add(old);
        await db.SaveChangesAsync();
        var cache = new JwtSigningKeyCache();
        var rotation = Rotation(db, cache);
        var before = DateTime.UtcNow;
        await rotation.RotateKeyAsync();
        Assert.True(old.ExpiresAt >= before.AddMinutes(Options().AccessTokenExpirationMinutes));
        Assert.False(old.IsActive);
    }

    private static KeyDbContext NewDatabase() => new(new DbContextOptionsBuilder<KeyDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static KeyRotationService Rotation(KeyDbContext db, JwtSigningKeyCache cache)
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(value => value.Set<JwtSigningKey>()).Returns(db.Set<JwtSigningKey>());
        context.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => db.SaveChangesAsync(ct));
        return new KeyRotationService(context.Object, NullLogger<KeyRotationService>.Instance, cache,
            Microsoft.Extensions.Options.Options.Create(Options()));
    }

    private static JwtTokenService Tokens(JwtOptions options, IKeyRotationService rotation, IJwtSigningKeyCache cache)
        => new(NullLogger<JwtTokenService>.Instance, Mock.Of<IRefreshTokenRepository>(),
            Mock.Of<IRefreshTokenHasher>(), new HttpContextAccessor(), Microsoft.Extensions.Options.Options.Create(options),
            keyRotationService: rotation, signingKeyCache: cache);

    private static Task<string> UserToken(JwtTokenService service)
        => service.GenerateAccessTokenAsync(Guid.NewGuid(), "user@example.invalid", ["Reader"], null);

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }

    private sealed class KeyDbContext(DbContextOptions<KeyDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<JwtSigningKey>();
    }
}
