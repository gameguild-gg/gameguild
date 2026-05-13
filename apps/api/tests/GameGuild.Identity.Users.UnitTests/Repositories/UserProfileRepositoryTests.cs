using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.Identity.Tenants;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Repositories;

public class UserProfileRepositoryTests
{
    [Fact]
    public async Task GetByUserIdAndGetById_ShouldExcludeDeletedProfiles()
    {
        await using var context = CreateContext();
        var repository = new UserProfileRepository(context);
        var activeUserId = Guid.NewGuid();
        var deletedUserId = Guid.NewGuid();
        var activeProfile = CreateProfile(activeUserId, "Active Designer", createdAt: new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        var deletedProfile = CreateProfile(deletedUserId, "Deleted Designer", deletedAt: new DateTime(2024, 9, 3, 0, 0, 0, DateTimeKind.Utc), createdAt: new DateTime(2024, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        await SeedProfilesAsync(context, activeProfile, deletedProfile);

        var fetchedByUserId = await repository.GetByUserIdAsync(activeUserId);
        var fetchedDeletedByUserId = await repository.GetByUserIdAsync(deletedUserId);
        var fetchedById = await repository.GetByIdAsync(activeProfile.Id);
        var fetchedDeletedById = await repository.GetByIdAsync(deletedProfile.Id);

        fetchedByUserId.Should().NotBeNull();
        fetchedByUserId!.DisplayName.Should().Be("Active Designer");
        fetchedDeletedByUserId.Should().BeNull();
        fetchedById.Should().NotBeNull();
        fetchedDeletedById.Should().BeNull();
    }

    [Fact]
    public async Task GetProfilesPagedAsync_ShouldApplySearchAndSortByDisplayNameDescending()
    {
        await using var context = CreateContext();
        var repository = new UserProfileRepository(context);

        await SeedProfilesAsync(
            context,
            CreateProfile(Guid.NewGuid(), "Alpha Designer", bio: "Product designer", location: "Sao Paulo", createdAt: new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateProfile(Guid.NewGuid(), "Beta Designer", bio: "UX designer", location: "Rio", createdAt: new DateTime(2024, 9, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateProfile(Guid.NewGuid(), "Gamma Engineer", bio: "Backend engineer", location: "Curitiba", createdAt: new DateTime(2024, 9, 3, 0, 0, 0, DateTimeKind.Utc)),
            CreateProfile(Guid.NewGuid(), "Deleted Designer", bio: "designer", location: "Recife", deletedAt: new DateTime(2024, 9, 4, 0, 0, 0, DateTimeKind.Utc), createdAt: new DateTime(2024, 9, 4, 0, 0, 0, DateTimeKind.Utc)));

        var (profiles, totalCount) = await repository.GetProfilesPagedAsync(
            search: "designer",
            sortBy: "displayName",
            sortDirection: "desc",
            pageNumber: 1,
            pageSize: 10);

        totalCount.Should().Be(2);
        profiles.Select(profile => profile.DisplayName).Should().Equal("Beta Designer", "Alpha Designer");
    }

    [Fact]
    public async Task GetProfilesPagedAsync_ShouldPaginateAndSortByUpdatedAtAscending()
    {
        await using var context = CreateContext();
        var repository = new UserProfileRepository(context);
        var first = CreateProfile(Guid.NewGuid(), "First", createdAt: new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        first.UpdatedAt = new DateTime(2024, 10, 2, 0, 0, 0, DateTimeKind.Utc);
        var second = CreateProfile(Guid.NewGuid(), "Second", createdAt: new DateTime(2024, 10, 3, 0, 0, 0, DateTimeKind.Utc));
        second.UpdatedAt = new DateTime(2024, 10, 4, 0, 0, 0, DateTimeKind.Utc);
        var third = CreateProfile(Guid.NewGuid(), "Third", createdAt: new DateTime(2024, 10, 5, 0, 0, 0, DateTimeKind.Utc));
        third.UpdatedAt = new DateTime(2024, 10, 6, 0, 0, 0, DateTimeKind.Utc);

        await SeedProfilesAsync(context, first, second, third);

        var (profiles, totalCount) = await repository.GetProfilesPagedAsync(
            search: null,
            sortBy: "updatedat",
            sortDirection: "asc",
            pageNumber: 2,
            pageSize: 1);

        totalCount.Should().Be(3);
        profiles.Should().ContainSingle();
        profiles[0].DisplayName.Should().Be("Second");
    }

    [Fact]
    public async Task GetProfilesPagedAsync_ShouldSearchAcrossBioAndLocation_AndSortByDisplayNameAscending()
    {
        await using var context = CreateContext();
        var repository = new UserProfileRepository(context);

        await SeedProfilesAsync(
            context,
            CreateProfile(Guid.NewGuid(), "Zeta", bio: "Frontend specialist", location: "Porto", createdAt: new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateProfile(Guid.NewGuid(), "Bravo", bio: "Kansei researcher", location: "Tokyo", createdAt: new DateTime(2024, 10, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateProfile(Guid.NewGuid(), "Charlie", bio: "Platform engineer", location: "Kansei Lab", createdAt: new DateTime(2024, 10, 3, 0, 0, 0, DateTimeKind.Utc)),
            CreateProfile(Guid.NewGuid(), "Deleted", bio: "kansei archive", location: "Nagoya", deletedAt: new DateTime(2024, 10, 4, 0, 0, 0, DateTimeKind.Utc), createdAt: new DateTime(2024, 10, 4, 0, 0, 0, DateTimeKind.Utc)));

        var (profiles, totalCount) = await repository.GetProfilesPagedAsync(
            search: "kansei",
            sortBy: "displayName",
            sortDirection: "asc",
            pageNumber: 1,
            pageSize: 10);

        totalCount.Should().Be(2);
        profiles.Select(profile => profile.DisplayName).Should().Equal("Bravo", "Charlie");
    }

    [Fact]
    public async Task GetProfilesPagedAsync_ShouldCoverRemainingSortBranches()
    {
        await using var context = CreateContext();
        var repository = new UserProfileRepository(context);
        var alpha = CreateProfile(Guid.NewGuid(), "Alpha", location: "Amsterdam", createdAt: new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        alpha.UpdatedAt = new DateTime(2024, 10, 30, 0, 0, 0, DateTimeKind.Utc);

        var mike = CreateProfile(Guid.NewGuid(), "Mike", location: "Berlin", createdAt: new DateTime(2024, 10, 2, 0, 0, 0, DateTimeKind.Utc));
        mike.UpdatedAt = new DateTime(2024, 10, 10, 0, 0, 0, DateTimeKind.Utc);

        var zulu = CreateProfile(Guid.NewGuid(), "Zulu", location: "Zurich", createdAt: new DateTime(2024, 10, 3, 0, 0, 0, DateTimeKind.Utc));
        zulu.UpdatedAt = new DateTime(2024, 10, 20, 0, 0, 0, DateTimeKind.Utc);

        await SeedProfilesAsync(context, alpha, mike, zulu);

        var (locationAscending, locationAscendingCount) = await repository.GetProfilesPagedAsync(null, "location", "asc", 1, 10);
        var (locationDescending, _) = await repository.GetProfilesPagedAsync(null, "location", "desc", 1, 10);
        var (updatedAtDescending, _) = await repository.GetProfilesPagedAsync(null, "updatedAt", "desc", 1, 10);
        var (createdAtAscending, _) = await repository.GetProfilesPagedAsync(null, "createdAt", "asc", 1, 10);
        var (createdAtDescending, _) = await repository.GetProfilesPagedAsync(null, "createdAt", "desc", 1, 10);
        var (fallbackAscending, _) = await repository.GetProfilesPagedAsync(null, null, null, 1, 10);
        var (fallbackDescending, _) = await repository.GetProfilesPagedAsync(null, "unsupported", "desc", 1, 10);

        locationAscendingCount.Should().Be(3);
        locationAscending.Select(profile => profile.DisplayName).Should().Equal("Alpha", "Mike", "Zulu");
        locationDescending.Select(profile => profile.DisplayName).Should().Equal("Zulu", "Mike", "Alpha");
        updatedAtDescending.Select(profile => profile.DisplayName).Should().Equal("Alpha", "Zulu", "Mike");
        createdAtAscending.Select(profile => profile.DisplayName).Should().Equal("Alpha", "Mike", "Zulu");
        createdAtDescending.Select(profile => profile.DisplayName).Should().Equal("Zulu", "Mike", "Alpha");
        fallbackAscending.Select(profile => profile.DisplayName).Should().Equal("Alpha", "Mike", "Zulu");
        fallbackDescending.Select(profile => profile.DisplayName).Should().Equal("Zulu", "Mike", "Alpha");
    }

    [Fact]
    public async Task AddUpdateDeleteAndSaveChanges_ShouldPersistAndSoftDeleteProfile()
    {
        await using var context = CreateContext();
        var repository = new UserProfileRepository(context);
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId, "Display");

        await SeedUserAsync(context, userId);
        await repository.AddAsync(profile);
        await repository.SaveChangesAsync();

        var fetched = await repository.GetByIdAsync(profile.Id);
        fetched.Should().NotBeNull();
        fetched!.DisplayName.Should().Be("Display");

        profile.UpdateBanner("https://example.com/banner.png");
        await repository.UpdateAsync(profile);
        await repository.SaveChangesAsync();

        (await repository.GetByIdAsync(profile.Id))!.BannerUrl.Should().Be("https://example.com/banner.png");

        profile.Version = 1;
        await repository.DeleteAsync(profile);
        await repository.SaveChangesAsync();

        (await repository.GetByIdAsync(profile.Id)).Should().BeNull();
        (await repository.GetByUserIdAsync(userId)).Should().BeNull();
    }

    private static UsersProfileRepositoryTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersProfileRepositoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new UsersProfileRepositoryTestDbContext(options);
    }

    private static async Task SeedProfilesAsync(UsersProfileRepositoryTestDbContext context, params UserProfile[] profiles)
    {
        foreach (var userId in profiles.Select(profile => profile.UserId).Distinct())
        {
            await SeedUserAsync(context, userId);
        }

        await context.UserProfiles.AddRangeAsync(profiles);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(UsersProfileRepositoryTestDbContext context, Guid userId)
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

    private static UserProfile CreateProfile(Guid userId, string displayName, string? bio = null, string? location = null, DateTime? deletedAt = null, DateTime? createdAt = null)
    {
        var profile = UserProfile.Create(userId, displayName);
        profile.Id = Guid.NewGuid();
        profile.Bio = bio;
        profile.Location = location;
        profile.CreatedAt = createdAt ?? new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        profile.UpdatedAt = profile.CreatedAt.AddHours(1);
        profile.Version = 1;
        profile.DeletedAt = deletedAt;
        return profile;
    }

    private sealed class UsersProfileRepositoryTestDbContext(DbContextOptions<UsersProfileRepositoryTestDbContext> options)
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
