using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class ServiceAccountPatchPersistenceTests
{
    [Theory]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("expiry")]
    [InlineData("scopes-and-expiry")]
    public async Task Patch_PersistsFieldsWhenReloadedInAnotherContext(string field)
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var id = Guid.NewGuid();
        var expiry = DateTime.UtcNow.AddDays(10);
        await using (var seed = new AccountDbContext(options))
        {
            seed.Add(new ServiceAccount
            {
                Id = id, Name = "Original", Description = "Original description",
                ClientId = Guid.NewGuid().ToString(), Scopes = "read:data"
            });
            await seed.SaveChangesAsync();
        }

        await using (var writer = new AccountDbContext(options))
        {
            var context = new Mock<IApplicationDbContext>();
            context.Setup(db => db.Set<ServiceAccount>()).Returns(writer.Set<ServiceAccount>());
            context.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) => writer.SaveChangesAsync(ct));
            var service = new ServiceAccountService(new ServiceAccountRepository(context.Object),
                Mock.Of<IRefreshTokenHasher>(), NullLogger<ServiceAccountService>.Instance);
            var handler = new ServiceAccountMutationCommandHandler(service, Mock.Of<IJwtTokenService>());

            var result = await handler.Handle(new PatchServiceAccountCommand(id,
                field == "name" ? "Renamed" : null,
                field == "description" ? "" : null,
                field == "scopes-and-expiry" ? "write:data" : null,
                field is "expiry" or "scopes-and-expiry" ? expiry : null), CancellationToken.None);

            Assert.True(result);
        }

        await using var reader = new AccountDbContext(options);
        var stored = await reader.Set<ServiceAccount>().SingleAsync(account => account.Id == id);
        Assert.Equal(field == "name" ? "Renamed" : "Original", stored.Name);
        Assert.Equal(field == "description" ? "" : "Original description", stored.Description);
        Assert.Equal(field == "scopes-and-expiry" ? "write:data" : "read:data", stored.Scopes);
        Assert.Equal(field is "expiry" or "scopes-and-expiry" ? expiry : (DateTime?)null, stored.ExpiresAt);
    }

    private sealed class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<ServiceAccount>();
    }
}
