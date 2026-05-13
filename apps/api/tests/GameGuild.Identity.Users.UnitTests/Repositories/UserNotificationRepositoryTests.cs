using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.Identity.Tenants;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Repositories;

public class UserNotificationRepositoryTests
{
    [Fact]
    public async Task GetByUserIdAsync_ShouldExcludeArchivedAndDeletedNotifications()
    {
        await using var context = CreateContext();
        var repository = new UserNotificationRepository(context);
        var userId = Guid.NewGuid();

        await SeedNotificationsAsync(
            context,
            CreateNotification(userId, "billing", "Newest", createdAt: new DateTime(2024, 4, 3, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "billing", "Archived", isArchived: true, createdAt: new DateTime(2024, 4, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "billing", "Deleted", deletedAt: new DateTime(2024, 4, 4, 0, 0, 0, DateTimeKind.Utc), createdAt: new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(Guid.NewGuid(), "billing", "Other User", createdAt: new DateTime(2024, 4, 5, 0, 0, 0, DateTimeKind.Utc)));

        var result = await repository.GetByUserIdAsync(userId, skip: 0, take: 10);

        result.Should().ContainSingle();
        result[0].Title.Should().Be("Newest");
    }

    [Fact]
    public async Task GetPagedByUserIdAsync_ShouldApplyFilters_AndSortByPriorityAscending()
    {
        await using var context = CreateContext();
        var repository = new UserNotificationRepository(context);
        var userId = Guid.NewGuid();
        var fromDate = new DateTime(2024, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2024, 5, 30, 0, 0, 0, DateTimeKind.Utc);

        await SeedNotificationsAsync(
            context,
            CreateNotification(userId, "billing", "Invoice low", content: "invoice generated", priority: NotificationPriority.Low, createdAt: new DateTime(2024, 5, 12, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "billing", "Invoice urgent", content: "invoice reminder", priority: NotificationPriority.Urgent, createdAt: new DateTime(2024, 5, 14, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "billing", "Invoice archived", content: "invoice archived", priority: NotificationPriority.High, isArchived: true, createdAt: new DateTime(2024, 5, 16, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "system", "System invoice", content: "invoice system", priority: NotificationPriority.High, createdAt: new DateTime(2024, 5, 18, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "billing", "Invoice read", content: "invoice already read", priority: NotificationPriority.High, isRead: true, createdAt: new DateTime(2024, 5, 20, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "billing", "Outside range", content: "invoice too old", priority: NotificationPriority.High, createdAt: new DateTime(2024, 4, 20, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(Guid.NewGuid(), "billing", "Other user", content: "invoice other", priority: NotificationPriority.High, createdAt: new DateTime(2024, 5, 22, 0, 0, 0, DateTimeKind.Utc)));

        var (notifications, totalCount) = await repository.GetPagedByUserIdAsync(
            userId,
            page: 1,
            pageSize: 10,
            search: "invoice",
            sortBy: "priority",
            sortDirection: "asc",
            isArchived: false,
            type: "billing",
            isRead: false,
            priority: null,
            fromDate: fromDate,
            toDate: toDate);

        totalCount.Should().Be(2);
        notifications.Select(notification => notification.Title).Should().Equal("Invoice low", "Invoice urgent");
    }

    [Fact]
    public async Task GetPagedByUserIdAsync_ShouldPaginate_AndSortByTitleDescending()
    {
        await using var context = CreateContext();
        var repository = new UserNotificationRepository(context);
        var userId = Guid.NewGuid();

        await SeedNotificationsAsync(
            context,
            CreateNotification(userId, "billing", "Alpha", createdAt: new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "billing", "Beta", createdAt: new DateTime(2024, 6, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "billing", "Gamma", createdAt: new DateTime(2024, 6, 3, 0, 0, 0, DateTimeKind.Utc)));

        var (notifications, totalCount) = await repository.GetPagedByUserIdAsync(
            userId,
            page: 2,
            pageSize: 1,
            search: null,
            sortBy: "title",
            sortDirection: "desc");

        totalCount.Should().Be(3);
        notifications.Should().ContainSingle();
        notifications[0].Title.Should().Be("Beta");
    }

    [Fact]
    public async Task GetPagedByUserIdAsync_ShouldCoverInvalidPriorityAndRemainingSortBranches()
    {
        await using var context = CreateContext();
        var repository = new UserNotificationRepository(context);
        var userId = Guid.NewGuid();

        await SeedNotificationsAsync(
            context,
            CreateNotification(userId, "system", "Bravo", priority: NotificationPriority.High, createdAt: new DateTime(2024, 6, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "billing", "Charlie", priority: NotificationPriority.Low, createdAt: new DateTime(2024, 6, 3, 0, 0, 0, DateTimeKind.Utc)),
            CreateNotification(userId, "alerts", "Alpha", priority: NotificationPriority.Urgent, createdAt: new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)));

        var (typeAscending, typeAscendingCount) = await repository.GetPagedByUserIdAsync(
            userId,
            page: 1,
            pageSize: 10,
            sortBy: "type",
            sortDirection: "asc",
            priority: "not-a-priority");

        var (typeDescending, _) = await repository.GetPagedByUserIdAsync(
            userId,
            page: 1,
            pageSize: 10,
            sortBy: "type",
            sortDirection: "desc");

        var (titleAscending, _) = await repository.GetPagedByUserIdAsync(
            userId,
            page: 1,
            pageSize: 10,
            sortBy: "title",
            sortDirection: "asc");

        var (createdAtAscending, _) = await repository.GetPagedByUserIdAsync(
            userId,
            page: 1,
            pageSize: 10,
            sortBy: "createdat",
            sortDirection: "asc");

        var (fallbackDescending, _) = await repository.GetPagedByUserIdAsync(
            userId,
            page: 1,
            pageSize: 10,
            sortBy: null,
            sortDirection: null);

        var (fallbackAscending, _) = await repository.GetPagedByUserIdAsync(
            userId,
            page: 1,
            pageSize: 10,
            sortBy: "unsupported",
            sortDirection: "asc");

        typeAscendingCount.Should().Be(3);
        typeAscending.Select(notification => notification.Title).Should().Equal("Alpha", "Charlie", "Bravo");
        typeDescending.Select(notification => notification.Title).Should().Equal("Bravo", "Charlie", "Alpha");
        titleAscending.Select(notification => notification.Title).Should().Equal("Alpha", "Bravo", "Charlie");
        createdAtAscending.Select(notification => notification.Title).Should().Equal("Alpha", "Bravo", "Charlie");
        fallbackDescending.Select(notification => notification.Title).Should().Equal("Charlie", "Bravo", "Alpha");
        fallbackAscending.Select(notification => notification.Title).Should().Equal("Alpha", "Bravo", "Charlie");
    }

    [Fact]
    public async Task GetUnreadCounts_ShouldExcludeArchivedAndDeletedNotifications()
    {
        await using var context = CreateContext();
        var repository = new UserNotificationRepository(context);
        var userId = Guid.NewGuid();

        await SeedNotificationsAsync(
            context,
            CreateNotification(userId, "billing", "Unread billing", isRead: false),
            CreateNotification(userId, "system", "Unread system", isRead: false),
            CreateNotification(userId, "billing", "Read billing", isRead: true),
            CreateNotification(userId, "billing", "Archived unread", isRead: false, isArchived: true),
            CreateNotification(userId, "billing", "Deleted unread", isRead: false, deletedAt: new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc)));

        var unreadCount = await repository.GetUnreadCountByUserIdAsync(userId);
        var unreadByType = await repository.GetUnreadCountByTypeAsync(userId);

        unreadCount.Should().Be(2);
        unreadByType.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["billing"] = 1,
            ["system"] = 1
        });
    }

    [Fact]
    public async Task AddUpdateDeleteAndGetByIds_ShouldPersistRepositoryOperations()
    {
        await using var context = CreateContext();
        var repository = new UserNotificationRepository(context);
        var userId = Guid.NewGuid();
        var notification = CreateNotification(userId, "billing", "Created");

        await repository.AddAsync(notification);
        await repository.SaveChangesAsync();

        var fetched = await repository.GetByIdAsync(notification.Id);
        fetched.Should().NotBeNull();
        fetched!.Title.Should().Be("Created");

        notification.Title = "Updated";
        await repository.UpdateAsync(notification);
        await repository.SaveChangesAsync();

        var byIds = await repository.GetByIdsAsync(userId, new List<Guid> { notification.Id, Guid.NewGuid() });
        byIds.Should().ContainSingle();
        byIds[0].Title.Should().Be("Updated");

        await repository.DeleteAsync(notification);
        await repository.SaveChangesAsync();

        (await repository.GetByIdAsync(notification.Id)).Should().BeNull();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkOnlyUnreadNotificationsForUser()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        await using var context = await CreateSqliteContextAsync(connection);
        var repository = new UserNotificationRepository(context);
        var userId = Guid.NewGuid();
        var unread = CreateNotification(userId, "billing", "Unread", isRead: false);
        var alreadyRead = CreateNotification(userId, "billing", "Read", isRead: true);
        var deleted = CreateNotification(userId, "billing", "Deleted", isRead: false, deletedAt: SystemClock.UtcNow);
        var otherUser = CreateNotification(Guid.NewGuid(), "billing", "Other", isRead: false);

        await SeedNotificationsAsync(context, unread, alreadyRead, deleted, otherUser);

        await repository.MarkAllAsReadAsync(userId);

        var refreshedUnread = await context.UserNotifications.AsNoTracking().FirstAsync(x => x.Id == unread.Id);
        var refreshedAlreadyRead = await context.UserNotifications.AsNoTracking().FirstAsync(x => x.Id == alreadyRead.Id);
        var refreshedDeleted = await context.UserNotifications.AsNoTracking().FirstAsync(x => x.Id == deleted.Id);
        var refreshedOtherUser = await context.UserNotifications.AsNoTracking().FirstAsync(x => x.Id == otherUser.Id);

        refreshedUnread.IsRead.Should().BeTrue();
        refreshedUnread.ReadAt.Should().NotBeNull();
        refreshedAlreadyRead.IsRead.Should().BeTrue();
        refreshedDeleted.IsRead.Should().BeFalse();
        refreshedOtherUser.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveAllAsync_ShouldArchiveOnlyOldUnarchivedNotificationsForUser()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        await using var context = await CreateSqliteContextAsync(connection);
        var repository = new UserNotificationRepository(context);
        var userId = Guid.NewGuid();
        var oldNotification = CreateNotification(userId, "billing", "Old", createdAt: SystemClock.UtcNow.AddDays(-40));
        var recentNotification = CreateNotification(userId, "billing", "Recent", createdAt: SystemClock.UtcNow.AddDays(-5));
        var archivedNotification = CreateNotification(userId, "billing", "Archived", isArchived: true, createdAt: SystemClock.UtcNow.AddDays(-50));
        var deletedNotification = CreateNotification(userId, "billing", "Deleted", createdAt: SystemClock.UtcNow.AddDays(-50), deletedAt: SystemClock.UtcNow);

        await SeedNotificationsAsync(context, oldNotification, recentNotification, archivedNotification, deletedNotification);

        await repository.ArchiveAllAsync(userId, olderThanDays: 30);

        var refreshedOld = await context.UserNotifications.AsNoTracking().FirstAsync(x => x.Id == oldNotification.Id);
        var refreshedRecent = await context.UserNotifications.AsNoTracking().FirstAsync(x => x.Id == recentNotification.Id);
        var refreshedArchived = await context.UserNotifications.AsNoTracking().FirstAsync(x => x.Id == archivedNotification.Id);
        var refreshedDeleted = await context.UserNotifications.AsNoTracking().FirstAsync(x => x.Id == deletedNotification.Id);

        refreshedOld.IsArchived.Should().BeTrue();
        refreshedOld.ArchivedAt.Should().NotBeNull();
        refreshedRecent.IsArchived.Should().BeFalse();
        refreshedArchived.IsArchived.Should().BeTrue();
        refreshedDeleted.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteArchivedAsync_ShouldDeleteOnlyOldArchivedNotificationsForUser()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        await using var context = await CreateSqliteContextAsync(connection);
        var repository = new UserNotificationRepository(context);
        var userId = Guid.NewGuid();
        var oldArchived = CreateNotification(userId, "billing", "Old Archived", isArchived: true, createdAt: SystemClock.UtcNow.AddDays(-120));
        oldArchived.ArchivedAt = SystemClock.UtcNow.AddDays(-100);
        var recentArchived = CreateNotification(userId, "billing", "Recent Archived", isArchived: true, createdAt: SystemClock.UtcNow.AddDays(-20));
        recentArchived.ArchivedAt = SystemClock.UtcNow.AddDays(-10);
        var otherUserArchived = CreateNotification(Guid.NewGuid(), "billing", "Other User Archived", isArchived: true, createdAt: SystemClock.UtcNow.AddDays(-120));
        otherUserArchived.ArchivedAt = SystemClock.UtcNow.AddDays(-100);

        await SeedNotificationsAsync(context, oldArchived, recentArchived, otherUserArchived);

        await repository.DeleteArchivedAsync(userId, olderThanDays: 90);

        (await context.UserNotifications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == oldArchived.Id)).Should().BeNull();
        (await context.UserNotifications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == recentArchived.Id)).Should().NotBeNull();
        (await context.UserNotifications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == otherUserArchived.Id)).Should().NotBeNull();
    }

    private static async Task<UsersRepositoryTestDbContext> CreateSqliteContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<UsersRepositoryTestDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new UsersRepositoryTestDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static UsersRepositoryTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersRepositoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new UsersRepositoryTestDbContext(options);
    }

    private static async Task SeedNotificationsAsync(UsersRepositoryTestDbContext context, params UserNotification[] notifications)
    {
        var userIds = notifications.Select(notification => notification.UserId).Distinct().ToList();
        var existingUserIds = await context.Users.Select(user => user.Id).ToListAsync();
        var missingUserIds = userIds.Except(existingUserIds).ToList();

        foreach (var missingUserId in missingUserIds)
        {
            context.Users.Add(new User
            {
                Id = missingUserId,
                Email = $"{missingUserId:N}@example.com",
                Name = $"User {missingUserId:N}",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc),
                Version = 1
            });
        }

        await context.UserNotifications.AddRangeAsync(notifications);
        await context.SaveChangesAsync();
    }

    private static UserNotification CreateNotification(
        Guid userId,
        string type,
        string title,
        string content = "message",
        NotificationPriority priority = NotificationPriority.Normal,
        bool isRead = false,
        bool isArchived = false,
        DateTime? deletedAt = null,
        DateTime? createdAt = null)
    {
        var notification = UserNotification.Create(userId, type, title, content, priority);
        notification.IsRead = isRead;
        notification.IsArchived = isArchived;
        notification.Metadata = "{}";
        notification.CreatedAt = createdAt ?? new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        notification.UpdatedAt = notification.CreatedAt.AddHours(1);
        notification.Version = 1;
        notification.DeletedAt = deletedAt;
        return notification;
    }

    private sealed class UsersRepositoryTestDbContext(DbContextOptions<UsersRepositoryTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<UserMetadata> UserMetadataSet => Set<UserMetadata>();
        public DbSet<UserPreferences> UserPreferencesSet => Set<UserPreferences>();
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
