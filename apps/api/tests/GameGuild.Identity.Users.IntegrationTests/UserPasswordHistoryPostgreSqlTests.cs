using GameGuild.API.Database;
using GameGuild.Identity.Users;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Users.Integration;

public sealed class UserPasswordHistoryPostgreSqlTests : IAsyncLifetime
{
    private EconomyPostgreSqlTestDatabase? _database;

    public async Task InitializeAsync() =>
        _database = await EconomyPostgreSqlTestDatabase.CreateAsync("user_password_history");

    public async Task DisposeAsync()
    {
        if (_database is not null) await _database.DisposeAsync();
    }

    [Fact]
    public async Task PasswordHistoryPersistsAndConcurrentPasswordChangesAreRejected()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_database!.ConnectionString)
            .Options;
        var userId = Guid.NewGuid();

        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            await setup.Set<User>().AddAsync(new User
            {
                Id = userId,
                Email = $"password-history-{userId:N}@example.test",
                Name = "Password History Test",
                PasswordHash = "hash-v1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await setup.SaveChangesAsync();
        }

        await using var firstContext = new ApplicationDbContext(options);
        await using var secondContext = new ApplicationDbContext(options);
        var firstUser = await firstContext.Set<User>().SingleAsync(user => user.Id == userId);
        var secondUser = await secondContext.Set<User>().SingleAsync(user => user.Id == userId);

        firstUser.SetPasswordHash("hash-v2");
        secondUser.SetPasswordHash("hash-v3");

        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync());

        await using var verificationContext = new ApplicationDbContext(options);
        var persistedUser = await verificationContext.Set<User>().SingleAsync(user => user.Id == userId);
        Assert.Equal("hash-v2", persistedUser.PasswordHash);
        Assert.Equal(new[] { "hash-v1" }, persistedUser.GetPasswordHistoryHashes());
    }
}
