using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using GameGuild.Identity.Tenants;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Configuration;

public class UsersConfigurationTests
{
    [Fact]
    public void UsersModelConfiguration_ShouldApplyAllModuleEntities()
    {
        using var context = CreateContext();
        var model = context.Model;

        model.FindEntityType(typeof(User)).Should().NotBeNull();
        model.FindEntityType(typeof(UserProfile)).Should().NotBeNull();
        model.FindEntityType(typeof(UserMetadata)).Should().NotBeNull();
        model.FindEntityType(typeof(UserPreferences)).Should().NotBeNull();
        model.FindEntityType(typeof(UserNotification)).Should().NotBeNull();

    }

    [Fact]
    public void UserConfiguration_ShouldConfigureRelationships_AndIgnoreComputedStatus()
    {
        using var context = CreateContext();
        var userEntity = context.Model.FindEntityType(typeof(User))!;

        userEntity.FindPrimaryKey()!.Properties.Should().ContainSingle().Which.Name.Should().Be(nameof(User.Id));
        userEntity.FindProperty(nameof(User.Status)).Should().BeNull();
        userEntity.FindNavigation(nameof(User.Profile))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        userEntity.FindNavigation(nameof(User.Metadata))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        userEntity.FindNavigation(nameof(User.Preferences))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        userEntity.FindNavigation(nameof(User.Notifications))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        userEntity.FindNavigation(nameof(User.TenantMemberships))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void UserNotificationConfiguration_ShouldConfigureIndexes_Constraints_AndRelationship()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(UserNotification))!;

        entityType.FindNavigation(nameof(UserNotification.User))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        entityType.FindProperty("UserId1").Should().BeNull();
        entityType.FindProperty(nameof(UserNotification.Metadata))!.IsNullable.Should().BeFalse();
        entityType.FindProperty(nameof(UserNotification.Type))!.GetMaxLength().Should().Be(50);
        entityType.FindProperty(nameof(UserNotification.Title))!.GetMaxLength().Should().Be(200);
        entityType.FindProperty(nameof(UserNotification.Content))!.GetMaxLength().Should().Be(2000);
        entityType.FindProperty(nameof(UserNotification.Source))!.GetMaxLength().Should().Be(100);
        entityType.FindProperty(nameof(UserNotification.RelatedEntityType))!.GetMaxLength().Should().Be(100);
        entityType.FindProperty(nameof(UserNotification.ActionUrl))!.GetMaxLength().Should().Be(500);

        entityType.GetIndexes().Should().Contain(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(UserNotification.UserId));
        entityType.GetIndexes().Should().Contain(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(UserNotification.Type));
        entityType.GetIndexes().Should().Contain(index =>
            index.Properties.Count == 2 &&
            index.Properties[0].Name == nameof(UserNotification.UserId) &&
            index.Properties[1].Name == nameof(UserNotification.IsRead));
        entityType.GetIndexes().Should().Contain(index =>
            index.Properties.Count == 2 &&
            index.Properties[0].Name == nameof(UserNotification.UserId) &&
            index.Properties[1].Name == nameof(UserNotification.IsArchived));
        entityType.GetIndexes().Should().Contain(index =>
            index.Properties.Count == 3 &&
            index.Properties[0].Name == nameof(UserNotification.UserId) &&
            index.Properties[1].Name == nameof(UserNotification.Type) &&
            index.Properties[2].Name == nameof(UserNotification.IsRead));
    }

    [Fact]
    public void UserProfileMetadataAndPreferencesConfigurations_ShouldConfigureUniqueUserRelationships()
    {
        using var context = CreateContext();
        var profileEntity = context.Model.FindEntityType(typeof(UserProfile))!;
        var metadataEntity = context.Model.FindEntityType(typeof(UserMetadata))!;
        var preferencesEntity = context.Model.FindEntityType(typeof(UserPreferences))!;

        profileEntity.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(UserProfile.UserId));
        profileEntity.FindNavigation(nameof(UserProfile.User))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        profileEntity.FindProperty(nameof(UserProfile.Visibility))!.Should().NotBeNull();
        profileEntity.FindProperty(nameof(UserProfile.DateOfBirth))!.Should().NotBeNull();

        metadataEntity.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(UserMetadata.UserId));
        metadataEntity.FindNavigation(nameof(UserMetadata.User))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        metadataEntity.FindProperty("UserId1").Should().BeNull();
        metadataEntity.FindProperty(nameof(UserMetadata.CustomFields))!.IsNullable.Should().BeFalse();
        metadataEntity.FindProperty(nameof(UserMetadata.Tags))!.IsNullable.Should().BeFalse();
        metadataEntity.FindProperty(nameof(UserMetadata.ExternalReferences))!.IsNullable.Should().BeFalse();

        preferencesEntity.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(UserPreferences.UserId));
        preferencesEntity.FindNavigation(nameof(UserPreferences.User))!.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        preferencesEntity.FindProperty("UserId1").Should().BeNull();
        preferencesEntity.FindProperty(nameof(UserPreferences.GeneralPreferences))!.IsNullable.Should().BeFalse();
        preferencesEntity.FindProperty(nameof(UserPreferences.NotificationPreferences))!.IsNullable.Should().BeFalse();
        preferencesEntity.FindProperty(nameof(UserPreferences.AccessibilityPreferences))!.IsNullable.Should().BeFalse();
        preferencesEntity.FindProperty(nameof(UserPreferences.PrivacyPreferences))!.IsNullable.Should().BeFalse();
    }

    private static UsersConfigurationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersConfigurationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UsersConfigurationTestDbContext(options);
    }

    private sealed class UsersConfigurationTestDbContext(DbContextOptions<UsersConfigurationTestDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<UserMetadata> UserMetadata => Set<UserMetadata>();
        public DbSet<UserPreferences> UserPreferencesSet => Set<UserPreferences>();
        public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
        public DbSet<TenantMember> TenantMembers => Set<TenantMember>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new UsersModelConfiguration().Configure(modelBuilder);
        }
    }
}