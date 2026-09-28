using GameGuild.API.Database;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace GameGuild.Tests.Authentication.Integration;

public sealed class RefreshTokenRotationPostgreSqlTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("gameguild_refresh_token_rotation")
        .WithUsername("test")
        .WithPassword("test")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task ConcurrentRotations_OnlyOneRequestClaimsTheRefreshToken()
    {
        var options = CreateOptions();
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var revokedAt = DateTime.UtcNow;

        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            await setup.Set<User>().AddAsync(new User
            {
                Id = userId,
                Email = $"refresh-rotation-{userId:N}@example.test",
                Name = "Refresh Token Rotation Test",
                PasswordHash = "test-hash",
                IsActive = true,
                CreatedAt = revokedAt.AddMinutes(-5),
                UpdatedAt = revokedAt.AddMinutes(-5)
            });
            await setup.Set<RefreshToken>().AddAsync(new RefreshToken
            {
                Id = tokenId,
                UserId = userId,
                Token = "expected-refresh-hash",
                CreatedByIp = "192.0.2.1",
                CreatedAt = revokedAt.AddMinutes(-1),
                UpdatedAt = revokedAt.AddMinutes(-1),
                ExpiresAt = revokedAt.AddDays(5)
            });
            await setup.SaveChangesAsync();
        }

        await using var firstContext = new ApplicationDbContext(options);
        await using var secondContext = new ApplicationDbContext(options);
        var firstRepository = new RefreshTokenRepository(firstContext);
        var secondRepository = new RefreshTokenRepository(secondContext);

        var results = await Task.WhenAll(
            firstRepository.TryRevokeForRotationAsync(
                tokenId,
                "expected-refresh-hash",
                "replacement-hash-a",
                revokedAt,
                "192.0.2.10",
                CancellationToken.None),
            secondRepository.TryRevokeForRotationAsync(
                tokenId,
                "expected-refresh-hash",
                "replacement-hash-b",
                revokedAt,
                "192.0.2.11",
                CancellationToken.None));

        Assert.Single(results, claimed => claimed);
        Assert.Single(results, claimed => !claimed);

        var winningIndex = Array.FindIndex(results, claimed => claimed);
        var expectedReplacementHash = winningIndex == 0 ? "replacement-hash-a" : "replacement-hash-b";
        var expectedIpAddress = winningIndex == 0 ? "192.0.2.10" : "192.0.2.11";

        await using var verification = new ApplicationDbContext(options);
        var persistedToken = await verification.Set<RefreshToken>().SingleAsync(token => token.Id == tokenId);
        Assert.True(persistedToken.IsRevoked);
        Assert.Equal(revokedAt, persistedToken.RevokedAt);
        Assert.Equal(expectedReplacementHash, persistedToken.ReplacedByToken);
        Assert.Equal(expectedIpAddress, persistedToken.RevokedByIp);
    }

    [Fact]
    public async Task RotationClaim_WithWrongHashLeavesTheActiveTokenUntouched()
    {
        var options = CreateOptions();
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            await setup.Set<User>().AddAsync(new User
            {
                Id = userId,
                Email = $"refresh-rotation-mismatch-{userId:N}@example.test",
                Name = "Refresh Token Hash Test",
                PasswordHash = "test-hash",
                IsActive = true,
                CreatedAt = now.AddMinutes(-5),
                UpdatedAt = now.AddMinutes(-5)
            });
            await setup.Set<RefreshToken>().AddAsync(new RefreshToken
            {
                Id = tokenId,
                UserId = userId,
                Token = "persisted-hash",
                CreatedByIp = "192.0.2.1",
                CreatedAt = now.AddMinutes(-1),
                UpdatedAt = now.AddMinutes(-1),
                ExpiresAt = now.AddDays(5)
            });
            await setup.SaveChangesAsync();
        }

        await using var context = new ApplicationDbContext(options);
        var repository = new RefreshTokenRepository(context);

        var claimed = await repository.TryRevokeForRotationAsync(
            tokenId,
            "different-hash",
            "replacement-hash",
            now,
            "192.0.2.10",
            CancellationToken.None);

        Assert.False(claimed);
        var persistedToken = await context.Set<RefreshToken>().AsNoTracking().SingleAsync(token => token.Id == tokenId);
        Assert.False(persistedToken.IsRevoked);
        Assert.Null(persistedToken.RevokedAt);
        Assert.Null(persistedToken.ReplacedByToken);
    }

    private DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }
}
