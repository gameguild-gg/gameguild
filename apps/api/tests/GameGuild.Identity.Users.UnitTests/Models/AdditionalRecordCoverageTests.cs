using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Models;

public class AdditionalRecordCoverageTests
{
    [Fact]
    public void MetadataRequestRecords_ShouldExposeProperties()
    {
        var customFields = JsonMap(new Dictionary<string, object?> { ["department"] = "engineering" });
        var updateCustomFields = new UpdateUserCustomFieldsRequest(customFields);
        var updateTags = new UpdateUserTagsRequest(new List<string> { "staff" }, new List<string> { "old" });
        var replaceTags = new ReplaceUserTagsRequest(new List<string> { "staff", "lead" });

        updateCustomFields.CustomFields["department"].GetString().Should().Be("engineering");
        updateTags.TagsToAdd.Should().Equal("staff");
        updateTags.TagsToRemove.Should().Equal("old");
        replaceTags.Tags.Should().Equal("staff", "lead");
    }

    [Fact]
    public void NotificationModelRecords_ShouldExposeProperties()
    {
        var notificationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var metadata = JsonMap(new Dictionary<string, object?> { ["invoiceId"] = 42 });
        var notification = new UserNotificationDto(
            notificationId,
            userId,
            "billing",
            "Invoice Ready",
            "message",
            "high",
            "finance",
            true,
            false,
            DateTimeOffset.UtcNow,
            null,
            null,
            "https://example.com/invoice",
            "View",
            null,
            metadata,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            new byte[] { 1, 2, 3 });
        var count = new UserNotificationCountDto(
            Total: 10,
            Unread: 3,
            Archived: 2,
            ByPriority: new Dictionary<string, int> { ["high"] = 4 },
            ByCategory: new Dictionary<string, int> { ["finance"] = 5 });
        var actionRequest = new ExecuteNotificationActionRequest("open", metadata);
        var actionResult = new NotificationActionResultDto(true, "done", "https://example.com", notification);
        var categorySettings = new NotificationCategorySettingsDto(true, true, false, false, "high");
        var deliverySettings = new UserNotificationDeliverySettingsDto(
            userId,
            EmailEnabled: true,
            PushEnabled: true,
            SmsEnabled: false,
            InAppEnabled: true,
            EmailFrequency: "daily",
            PushFrequency: "instant",
            QuietHoursStart: new TimeOnly(22, 0),
            QuietHoursEnd: new TimeOnly(6, 0),
            TimeZone: "UTC",
            CategorySettings: new Dictionary<string, NotificationCategorySettingsDto> { ["billing"] = categorySettings });
        var updateDelivery = new UpdateUserNotificationDeliverySettingsRequest(
            EmailEnabled: false,
            PushEnabled: true,
            TimeZone: "America/Sao_Paulo",
            CategorySettings: new Dictionary<string, NotificationCategorySettingsDto> { ["billing"] = categorySettings });
        var filterCriteria = new NotificationFilterCriteria(
            Categories: new List<string> { "finance" },
            Priorities: new List<string> { "high" },
            Types: new List<string> { "billing" },
            IsRead: false,
            IsArchived: false,
            DateFrom: DateTimeOffset.UtcNow.AddDays(-7),
            DateTo: DateTimeOffset.UtcNow);

        notification.Metadata["invoiceId"].GetInt32().Should().Be(42);
        notification.CreatedAt.Should().NotBe(default);
        notification.UpdatedAt.Should().NotBeNull();
        notification.Version.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        count.Total.Should().Be(10);
        count.Unread.Should().Be(3);
        count.Archived.Should().Be(2);
        count.ByPriority["high"].Should().Be(4);
        count.ByCategory["finance"].Should().Be(5);
        actionRequest.Parameters!["invoiceId"].GetInt32().Should().Be(42);
        actionResult.Success.Should().BeTrue();
        actionResult.Message.Should().Be("done");
        actionResult.RedirectUrl.Should().Be("https://example.com");
        actionRequest.ActionId.Should().Be("open");
        actionResult.UpdatedNotification.Should().Be(notification);
        deliverySettings.UserId.Should().Be(userId);
        deliverySettings.EmailEnabled.Should().BeTrue();
        deliverySettings.PushEnabled.Should().BeTrue();
        deliverySettings.SmsEnabled.Should().BeFalse();
        deliverySettings.InAppEnabled.Should().BeTrue();
        deliverySettings.EmailFrequency.Should().Be("daily");
        deliverySettings.PushFrequency.Should().Be("instant");
        deliverySettings.QuietHoursStart.Should().Be(new TimeOnly(22, 0));
        deliverySettings.QuietHoursEnd.Should().Be(new TimeOnly(6, 0));
        deliverySettings.TimeZone.Should().Be("UTC");
        deliverySettings.CategorySettings["billing"].Priority.Should().Be("high");
        updateDelivery.EmailEnabled.Should().BeFalse();
        updateDelivery.PushEnabled.Should().BeTrue();
        updateDelivery.SmsEnabled.Should().BeNull();
        updateDelivery.InAppEnabled.Should().BeNull();
        updateDelivery.EmailFrequency.Should().BeNull();
        updateDelivery.PushFrequency.Should().BeNull();
        updateDelivery.QuietHoursStart.Should().BeNull();
        updateDelivery.QuietHoursEnd.Should().BeNull();
        updateDelivery.TimeZone.Should().Be("America/Sao_Paulo");
        updateDelivery.CategorySettings!["billing"].Enabled.Should().BeTrue();
        filterCriteria.Categories.Should().Equal("finance");
        filterCriteria.Priorities.Should().Equal("high");
        filterCriteria.Types.Should().Equal("billing");
        filterCriteria.IsRead.Should().BeFalse();
        filterCriteria.IsArchived.Should().BeFalse();
        filterCriteria.DateFrom.Should().NotBeNull();
        filterCriteria.DateTo.Should().NotBeNull();
    }

    [Fact]
    public void PreferenceDtoRecords_ShouldExposeProperties()
    {
        var quietHours = JsonMap(new Dictionary<string, object?> { ["start"] = "22:00" });
        var categories = JsonMap(new Dictionary<string, object?> { ["billing"] = true });
        var customSettings = JsonMap(new Dictionary<string, object?> { ["fontFamily"] = "Atkinson" });
        var dataCollection = JsonMap(new Dictionary<string, object?> { ["analytics"] = true });
        var thirdParty = JsonMap(new Dictionary<string, object?> { ["partners"] = false });

        var notificationPreferences = new UserNotificationPreferencesDto(
            EmailEnabled: true,
            PushEnabled: true,
            SmsEnabled: false,
            InAppEnabled: true,
            Frequency: "daily",
            QuietHours: quietHours,
            CategoryPreferences: categories);
        var accessibilityPreferences = new UserAccessibilityPreferencesDto(
            HighContrast: true,
            LargeText: false,
            ScreenReader: false,
            ReducedMotion: true,
            KeyboardNavigation: true,
            FontSize: 18,
            ColorScheme: "dark",
            CustomSettings: customSettings);
        var privacyPreferences = new UserPrivacyPreferencesDto(
            ProfileVisibility: "friends",
            ActivityTracking: false,
            DataCollection: dataCollection,
            ThirdPartySharing: thirdParty,
            MarketingEmails: false,
            AnalyticsCookies: true,
            PersonalizedContent: false,
            CustomSettings: JsonMap(new Dictionary<string, object?> { ["shareLocation"] = false }));

        notificationPreferences.EmailEnabled.Should().BeTrue();
        notificationPreferences.PushEnabled.Should().BeTrue();
        notificationPreferences.SmsEnabled.Should().BeFalse();
        notificationPreferences.InAppEnabled.Should().BeTrue();
        notificationPreferences.Frequency.Should().Be("daily");
        notificationPreferences.QuietHours["start"].GetString().Should().Be("22:00");
        notificationPreferences.CategoryPreferences["billing"].GetBoolean().Should().BeTrue();
        accessibilityPreferences.HighContrast.Should().BeTrue();
        accessibilityPreferences.LargeText.Should().BeFalse();
        accessibilityPreferences.ScreenReader.Should().BeFalse();
        accessibilityPreferences.ReducedMotion.Should().BeTrue();
        accessibilityPreferences.KeyboardNavigation.Should().BeTrue();
        accessibilityPreferences.FontSize.Should().Be(18);
        accessibilityPreferences.ColorScheme.Should().Be("dark");
        accessibilityPreferences.CustomSettings["fontFamily"].GetString().Should().Be("Atkinson");
        privacyPreferences.ProfileVisibility.Should().Be("friends");
        privacyPreferences.ActivityTracking.Should().BeFalse();
        privacyPreferences.DataCollection["analytics"].GetBoolean().Should().BeTrue();
        privacyPreferences.ThirdPartySharing["partners"].GetBoolean().Should().BeFalse();
        privacyPreferences.MarketingEmails.Should().BeFalse();
        privacyPreferences.AnalyticsCookies.Should().BeTrue();
        privacyPreferences.PersonalizedContent.Should().BeFalse();
        privacyPreferences.CustomSettings["shareLocation"].GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void UserExistsResponse_ShouldExposeProperties()
    {
        var userId = Guid.NewGuid();
        var response = new UserExistsResponse(true, "user@example.com", userId);

        response.Exists.Should().BeTrue();
        response.Email.Should().Be("user@example.com");
        response.UserId.Should().Be(userId);
    }

    [Fact]
    public void ProfileAssetAndRoleRecords_ShouldExposeProperties()
    {
        var uploadedAt = DateTimeOffset.UtcNow;
        var avatar = new UserAvatarDto("https://example.com/avatar.png", uploadedAt, 1024, "image/png");
        var uploadAvatar = new UploadUserAvatarRequest("base64-avatar", "image/png", "avatar.png");
        var banner = new UserBannerDto("https://example.com/banner.png", uploadedAt, 2048, "image/png");
        var uploadBanner = new UploadUserBannerRequest("base64-banner", "image/png", "banner.png");
        var roleAssignment = new UserRoleAssignment(Guid.NewGuid(), "Admin");
        var metadata = new UsersMetadataDto(10, 7, 2, 1, "etag-1");
        var deliverySettingsQuery = new GetUserNotificationDeliverySettingsQuery(Guid.NewGuid());

        avatar.FileSize.Should().Be(1024);
        uploadAvatar.FileName.Should().Be("avatar.png");
        banner.ContentType.Should().Be("image/png");
        uploadBanner.ImageData.Should().Be("base64-banner");
        roleAssignment.Role.Should().Be("Admin");
        metadata.ActiveCount.Should().Be(7);
        deliverySettingsQuery.UserId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void DtoAndRequestRecords_ShouldExposeAllProperties()
    {
        var userId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(5);
        var generalPreferences = JsonMap(new Dictionary<string, object?> { ["theme"] = "dark" });
        var notificationPreferences = JsonMap(new Dictionary<string, object?> { ["emailEnabled"] = true });
        var accessibilityPreferences = JsonMap(new Dictionary<string, object?> { ["fontSize"] = 18 });
        var privacyPreferences = JsonMap(new Dictionary<string, object?> { ["profileVisibility"] = "friends" });
        var localizationPreferences = JsonMap(new Dictionary<string, object?> { ["language"] = "pt-BR" });

        var profileDto = new UserProfileDto(
            Guid.NewGuid(),
            userId,
            "Display",
            "Bio",
            "Sao Paulo",
            "https://example.com",
            "Engineer",
            "GameGuild",
            "https://example.com/avatar.png",
            "https://example.com/banner.png",
            "America/Sao_Paulo",
            "pt-BR",
            "friends",
            true,
            false,
            createdAt,
            updatedAt,
            new byte[] { 1, 2 });
        var updateProfile = new UpdateUserProfileRequest(
            DisplayName: "Display",
            Bio: "Bio",
            Location: "Sao Paulo",
            Website: "https://example.com",
            JobTitle: "Engineer",
            Company: "GameGuild",
            TimeZone: "America/Sao_Paulo",
            Language: "pt-BR",
            ProfileVisibility: "friends",
            ShowEmail: true,
            ShowLocation: false);
        var notificationDto = new UserNotificationDto(
            Guid.NewGuid(),
            userId,
            "billing",
            "Invoice",
            "Message",
            "high",
            "finance",
            true,
            false,
            createdAt,
            null,
            null,
            "https://example.com/action",
            "Open",
            "https://example.com/image.png",
            JsonMap(new Dictionary<string, object?> { ["invoiceId"] = 42 }),
            createdAt,
            updatedAt,
            new byte[] { 3, 4 });
        var preferencesDto = new UserPreferencesDto(
            Guid.NewGuid(),
            userId,
            generalPreferences,
            notificationPreferences,
            accessibilityPreferences,
            privacyPreferences,
            localizationPreferences,
            createdAt,
            updatedAt,
            new byte[] { 5, 6 });

        profileDto.Id.Should().NotBe(Guid.Empty);
        profileDto.UserId.Should().Be(userId);
        profileDto.DisplayName.Should().Be("Display");
        profileDto.Bio.Should().Be("Bio");
        profileDto.Location.Should().Be("Sao Paulo");
        profileDto.Website.Should().Be("https://example.com");
        profileDto.JobTitle.Should().Be("Engineer");
        profileDto.Company.Should().Be("GameGuild");
        profileDto.AvatarUrl.Should().Be("https://example.com/avatar.png");
        profileDto.BannerUrl.Should().Be("https://example.com/banner.png");
        profileDto.TimeZone.Should().Be("America/Sao_Paulo");
        profileDto.Language.Should().Be("pt-BR");
        profileDto.ProfileVisibility.Should().Be("friends");
        profileDto.ShowEmail.Should().BeTrue();
        profileDto.ShowLocation.Should().BeFalse();
        profileDto.CreatedAt.Should().Be(createdAt);
        profileDto.UpdatedAt.Should().Be(updatedAt);
        profileDto.Version.Should().BeEquivalentTo(new byte[] { 1, 2 });

        updateProfile.DisplayName.Should().Be("Display");
        updateProfile.Bio.Should().Be("Bio");
        updateProfile.Location.Should().Be("Sao Paulo");
        updateProfile.Website.Should().Be("https://example.com");
        updateProfile.JobTitle.Should().Be("Engineer");
        updateProfile.Company.Should().Be("GameGuild");
        updateProfile.TimeZone.Should().Be("America/Sao_Paulo");
        updateProfile.Language.Should().Be("pt-BR");
        updateProfile.ProfileVisibility.Should().Be("friends");
        updateProfile.ShowEmail.Should().BeTrue();
        updateProfile.ShowLocation.Should().BeFalse();

        notificationDto.Id.Should().NotBe(Guid.Empty);
        notificationDto.UserId.Should().Be(userId);
        notificationDto.Type.Should().Be("billing");
        notificationDto.Title.Should().Be("Invoice");
        notificationDto.Message.Should().Be("Message");
        notificationDto.Priority.Should().Be("high");
        notificationDto.Category.Should().Be("finance");
        notificationDto.IsRead.Should().BeTrue();
        notificationDto.IsArchived.Should().BeFalse();
        notificationDto.ReadAt.Should().Be(createdAt);
        notificationDto.ArchivedAt.Should().BeNull();
        notificationDto.ExpiresAt.Should().BeNull();
        notificationDto.ActionUrl.Should().Be("https://example.com/action");
        notificationDto.ActionText.Should().Be("Open");
        notificationDto.ImageUrl.Should().Be("https://example.com/image.png");
        notificationDto.Metadata["invoiceId"].GetInt32().Should().Be(42);
        notificationDto.CreatedAt.Should().Be(createdAt);
        notificationDto.UpdatedAt.Should().Be(updatedAt);
        notificationDto.Version.Should().BeEquivalentTo(new byte[] { 3, 4 });

        preferencesDto.Id.Should().NotBe(Guid.Empty);
        preferencesDto.UserId.Should().Be(userId);
        preferencesDto.GeneralPreferences["theme"].GetString().Should().Be("dark");
        preferencesDto.NotificationPreferences["emailEnabled"].GetBoolean().Should().BeTrue();
        preferencesDto.AccessibilityPreferences["fontSize"].GetInt32().Should().Be(18);
        preferencesDto.PrivacyPreferences["profileVisibility"].GetString().Should().Be("friends");
        preferencesDto.LocalizationPreferences["language"].GetString().Should().Be("pt-BR");
        preferencesDto.CreatedAt.Should().Be(createdAt);
        preferencesDto.UpdatedAt.Should().Be(updatedAt);
        preferencesDto.Version.Should().BeEquivalentTo(new byte[] { 5, 6 });
    }

    [Fact]
    public void UserNotFoundException_Constructors_ShouldExposeProperties()
    {
        var userId = Guid.NewGuid();
        var defaultException = new UserNotFoundException();
        var emailException = new UserNotFoundException("user@example.com");
        var inner = new InvalidOperationException("missing");
        var custom = new UserNotFoundException("custom message", inner);
        var byId = new UserNotFoundException(userId);

        defaultException.Message.Should().Be("User was not found.");
        emailException.Message.Should().Be("User with email user@example.com was not found.");
        emailException.Email.Should().Be("user@example.com");
        custom.Message.Should().Be("custom message");
        custom.InnerException.Should().Be(inner);
        byId.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserAlreadyExistsException_Constructors_ShouldSetMessagesAndEmail()
    {
        var defaultException = new UserAlreadyExistsException();
        var emailException = new UserAlreadyExistsException("user@example.com");
        var inner = new InvalidOperationException("duplicate");
        var custom = new UserAlreadyExistsException("custom message", inner);

        defaultException.Message.Should().Be("User already exists.");
        emailException.Message.Should().Be("User with email user@example.com already exists.");
        emailException.Email.Should().Be("user@example.com");
        custom.Message.Should().Be("custom message");
        custom.InnerException.Should().Be(inner);
    }

    [Fact]
    public void UserLifecycleEvents_ShouldExposeMetadata()
    {
        var before = DateTimeOffset.UtcNow;
        var activated = new UserActivatedNotification(Guid.NewGuid(), "active@example.com", "Active User");
        var created = new UserCreatedNotification(Guid.NewGuid(), "created@example.com", "Created User");
        var deactivated = new UserDeactivatedNotification(Guid.NewGuid(), "deactivated@example.com", "Deactivated User");
        var updated = new UserUpdatedNotification(Guid.NewGuid(), "Updated User", "+15550000");
        var after = DateTimeOffset.UtcNow;

        activated.EventId.Should().NotBe(Guid.Empty);
        activated.OccurredAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        activated.Version.Should().Be(1);

        created.Email.Should().Be("created@example.com");
        deactivated.Name.Should().Be("Deactivated User");
        updated.PhoneNumber.Should().Be("+15550000");
        updated.Version.Should().Be(1);
    }
}