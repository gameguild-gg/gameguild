using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.Identity.Tenants;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Repositories;

public class UserMetadataAndPreferencesRepositoryTests
{
    [Fact]
    public async Task UserMetadataRepository_ShouldPersistReadAndSoftDeleteMetadata()
    {
        await using var context = CreateContext();
        var repository = new UserMetadataRepository(context);
        var userId = Guid.NewGuid();
        var metadata = UserMetadata.Create(userId, new Dictionary<string, object?> { ["department"] = "engineering" }, new List<string> { "staff" });
        metadata.Version = 1;

        await SeedUserAsync(context, userId);
        await repository.AddAsync(metadata);
        await repository.SaveChangesAsync();

        var fetchedByUserId = await repository.GetByUserIdAsync(userId);
        var fetchedById = await repository.GetByIdAsync(metadata.Id);

        fetchedByUserId.Should().NotBeNull();
        fetchedById.Should().NotBeNull();
        fetchedByUserId!.GetTags().Should().Contain("staff");

        metadata.UpdateNotes("updated");
        await repository.UpdateAsync(metadata);
        await repository.SaveChangesAsync();

        (await repository.GetByIdAsync(metadata.Id))!.Notes.Should().Be("updated");

        await repository.DeleteAsync(metadata);
        await repository.SaveChangesAsync();

        (await repository.GetByUserIdAsync(userId)).Should().BeNull();
        (await repository.GetByIdAsync(metadata.Id)).Should().BeNull();
    }

    [Fact]
    public async Task UserPreferencesRepository_ShouldPersistReadAndSoftDeletePreferences()
    {
        await using var context = CreateContext();
        var repository = new UserPreferencesRepository(context);
        var userId = Guid.NewGuid();
        var preferences = UserPreferences.Create(userId);
        preferences.Version = 1;
        preferences.SetGeneralPreferences(new Dictionary<string, object?> { ["theme"] = "dark" });

        await SeedUserAsync(context, userId);
        await repository.AddAsync(preferences);
        await repository.SaveChangesAsync();

        var fetchedByUserId = await repository.GetByUserIdAsync(userId);
        var fetchedById = await repository.GetByIdAsync(preferences.Id);

        fetchedByUserId.Should().NotBeNull();
        fetchedById.Should().NotBeNull();
        ((System.Text.Json.JsonElement)fetchedByUserId!.GetGeneralPreferences()["theme"]!).GetString().Should().Be("dark");

        preferences.SetPrivacyPreferences(new Dictionary<string, object?> { ["profileVisibility"] = "friends" });
        await repository.UpdateAsync(preferences);
        await repository.SaveChangesAsync();

        ((System.Text.Json.JsonElement)(await repository.GetByIdAsync(preferences.Id))!.GetPrivacyPreferences()["profileVisibility"]!).GetString().Should().Be("friends");

        await repository.DeleteAsync(preferences);
        await repository.SaveChangesAsync();

        (await repository.GetByUserIdAsync(userId)).Should().BeNull();
        (await repository.GetByIdAsync(preferences.Id)).Should().BeNull();
    }

    [Fact]
    public async Task UserPreferencesRepository_UpdateAndDelete_ShouldAttachDetachedPreferences()
    {
        await using var context = CreateContext();
        var repository = new UserPreferencesRepository(context);
        var userId = Guid.NewGuid();
        var preferences = UserPreferences.Create(userId);
        preferences.Version = 1;

        await SeedUserAsync(context, userId);
        await repository.AddAsync(preferences);
        await repository.SaveChangesAsync();

        context.Entry(preferences).State = EntityState.Detached;
        preferences.SetGeneralPreferences(new Dictionary<string, object?> { ["density"] = "compact" });

        await repository.UpdateAsync(preferences);
        context.Entry(preferences).State.Should().Be(EntityState.Modified);
        await repository.SaveChangesAsync();

        context.Entry(preferences).State = EntityState.Detached;

        await repository.DeleteAsync(preferences);
        preferences.DeletedAt.Should().NotBeNull();
        context.Entry(preferences).State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public async Task UserPreferencesRepository_UpdateAndDelete_ShouldUseSetUpdate_ForNonDbContextAbstraction()
    {
        var preferences = UserPreferences.Create(Guid.NewGuid());
        preferences.Version = 1;
        var set = new Mock<DbSet<UserPreferences>>();
        var context = new Mock<IApplicationDbContext>();
        context.Setup(database => database.Set<UserPreferences>()).Returns(set.Object);
        var repository = new UserPreferencesRepository(context.Object);

        await repository.UpdateAsync(preferences);
        await repository.DeleteAsync(preferences);

        set.Verify(dbSet => dbSet.Update(preferences), Times.Exactly(2));
        preferences.DeletedAt.Should().NotBeNull();
    }

    private static UsersMetadataPreferencesTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersMetadataPreferencesTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new UsersMetadataPreferencesTestDbContext(options);
    }

    private static async Task SeedUserAsync(UsersMetadataPreferencesTestDbContext context, Guid userId)
    {
        if (await context.Users.AnyAsync(user => user.Id == userId))
        {
            return;
        }

        context.Users.Add(new User
        {
            Id = userId,
            Email = $"{userId:N}@example.com",
            Name = "Test User",
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc),
            Version = 1
        });

        await context.SaveChangesAsync();
    }

    private sealed class UsersMetadataPreferencesTestDbContext(DbContextOptions<UsersMetadataPreferencesTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<UserMetadata> UserMetadataSet => Set<UserMetadata>();
        public DbSet<UserPreferences> UserPreferencesSet => Set<UserPreferences>();
        public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new UsersModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Mock.Of<IDbContextTransaction>());
        }
    }
}
