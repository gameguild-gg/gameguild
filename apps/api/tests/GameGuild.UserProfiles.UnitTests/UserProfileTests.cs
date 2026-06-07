using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Localization;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Tenants;
using GameGuild.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit;

/// <summary>
/// Unit tests for the UserProfile entity
/// </summary>
public class UserProfileTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Default_Values()
    {
        // Act
        var userProfile = new UserProfile();

        // Assert
        userProfile.Id.Should().NotBeEmpty();
        userProfile.DisplayName.Should().BeNull();
        userProfile.AccessLevel.Should().Be(AccessLevel.Private);
        userProfile.Localizations.Should().BeEmpty();
        userProfile.IsNew.Should().BeTrue();
        userProfile.IsDeleted.Should().BeFalse();
        userProfile.IsGlobal.Should().BeTrue(); // Tenant is null by default
    }

    [Fact]
    public void DisplayName_Should_Be_Settable()
    {
        // Arrange
        var userProfile = new UserProfile();
        const string displayName = "John Doe";

        // Act
        userProfile.DisplayName = displayName;

        // Assert
        userProfile.DisplayName.Should().Be(displayName);
    }

    [Fact]
    public void DisplayName_Should_Accept_Null_Value()
    {
        // Arrange
        var userProfile = new UserProfile { DisplayName = "John Doe" };

        // Act
        userProfile.DisplayName = null;

        // Assert
        userProfile.DisplayName.Should().BeNull();
    }

    [Fact]
    public void DisplayName_Should_Accept_Empty_String()
    {
        // Arrange
        var userProfile = new UserProfile();

        // Act
        userProfile.DisplayName = string.Empty;

        // Assert
        userProfile.DisplayName.Should().BeEmpty();
    }

    [Fact]
    public void DisplayName_Should_Accept_Long_String_Within_MaxLength()
    {
        // Arrange
        var userProfile = new UserProfile();
        var displayName = new string('A', 100); // MaxLength is 100

        // Act
        userProfile.DisplayName = displayName;

        // Assert
        userProfile.DisplayName.Should().Be(displayName);
        userProfile.DisplayName.Length.Should().Be(100);
    }

    [Fact]
    public void UserProfile_Should_Inherit_From_Resource()
    {
        // Arrange & Act
        var userProfile = new UserProfile();

        // Assert
        userProfile.Should().BeAssignableTo<Resource>();
    }

    [Fact]
    public void UserProfile_Should_Inherit_EntityBase_Properties()
    {
        // Arrange & Act
        var userProfile = new UserProfile();

        // Assert
        userProfile.Should().BeAssignableTo<EntityBase>();
        userProfile.Id.Should().NotBeEmpty();
        userProfile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        userProfile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        userProfile.Version.Should().Be(0);
        userProfile.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void UserProfile_Should_Support_Soft_Delete()
    {
        // Arrange
        var userProfile = new UserProfile();
        userProfile.Version = 1; // Simulate persisted entity

        // Act
        userProfile.SoftDelete();

        // Assert
        userProfile.IsDeleted.Should().BeTrue();
        userProfile.DeletedAt.Should().NotBeNull();
        userProfile.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UserProfile_Should_Support_Restore()
    {
        // Arrange
        var userProfile = new UserProfile();
        userProfile.Version = 1; // Simulate persisted entity
        userProfile.SoftDelete();

        // Act
        userProfile.Restore();

        // Assert
        userProfile.IsDeleted.Should().BeFalse();
        userProfile.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void UserProfile_Should_Update_Timestamp_On_Touch()
    {
        // Arrange
        var userProfile = new UserProfile();
        var originalUpdatedAt = userProfile.UpdatedAt;
        Thread.Sleep(1); // Ensure time difference

        // Act
        userProfile.Touch();

        // Assert
        userProfile.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UserProfile_Should_Support_Access_Level_Configuration()
    {
        // Arrange
        var userProfile = new UserProfile();

        // Act & Assert - Test each access level
        userProfile.AccessLevel = AccessLevel.Public;
        userProfile.AccessLevel.Should().Be(AccessLevel.Public);

        userProfile.AccessLevel = AccessLevel.Private;
        userProfile.AccessLevel.Should().Be(AccessLevel.Private);

        userProfile.AccessLevel = AccessLevel.Restricted;
        userProfile.AccessLevel.Should().Be(AccessLevel.Restricted);
    }

    [Fact]
    public void UserProfile_Should_Support_Localization()
    {
        // Arrange
        var userProfile = new UserProfile();

        // Act
        var localization = userProfile.AddLocalization(
            "DisplayName",
            "João Silva",
            new Language { Code = "pt-BR", Name = "Portuguese (Brazil)" }
        );

        // Assert
        userProfile.Localizations.Should().HaveCount(1);
        userProfile.Localizations.Should().Contain(localization);
        localization.FieldName.Should().Be("DisplayName");
        localization.Content.Should().Be("João Silva");
        localization.Language.Code.Should().Be("pt-BR");
    }

    [Fact]
    public void UserProfile_Should_Support_Multiple_Localizations()
    {
        // Arrange
        var userProfile = new UserProfile();
        var englishLang = new Language { Code = "en-US", Name = "English (US)" };
        var spanishLang = new Language { Code = "es-ES", Name = "Spanish (Spain)" };

        // Act
        var englishLocalization = userProfile.AddLocalization("DisplayName", "John Doe", englishLang);
        var spanishLocalization = userProfile.AddLocalization("DisplayName", "Juan Pérez", spanishLang);

        // Assert
        userProfile.Localizations.Should().HaveCount(2);
        userProfile.Localizations.Should().Contain(englishLocalization);
        userProfile.Localizations.Should().Contain(spanishLocalization);
    }

    [Fact]
    public void UserProfile_Should_Support_Tenant_Assignment()
    {
        // Arrange
        var userProfile = new UserProfile();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant"
        };

        // Act
        userProfile.AssignToTenant(tenant);

        // Assert
        userProfile.Tenant.Should().Be(tenant);
        userProfile.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void UserProfile_Should_Support_Global_Assignment()
    {
        // Arrange
        var userProfile = new UserProfile();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant"
        };
        userProfile.AssignToTenant(tenant);

        // Act
        userProfile.MakeGlobal();

        // Assert
        userProfile.Tenant.Should().BeNull();
        userProfile.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void UserProfile_Should_Support_Domain_Events()
    {
        // Arrange
        var userProfile = new UserProfile();
        var domainEvent = new TestDomainEvent();

        // Act
        userProfile.AddDomainEvent(domainEvent);

        // Assert
        userProfile.DomainEvents.Should().HaveCount(1);
        userProfile.DomainEvents.Should().Contain(domainEvent);
    }

    [Fact]
    public void UserProfile_Should_Clear_Domain_Events()
    {
        // Arrange
        var userProfile = new UserProfile();
        userProfile.AddDomainEvent(new TestDomainEvent());
        userProfile.AddDomainEvent(new TestDomainEvent());

        // Act
        userProfile.ClearDomainEvents();

        // Assert
        userProfile.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UserProfile_Should_Remove_Specific_Domain_Event()
    {
        // Arrange
        var userProfile = new UserProfile();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        userProfile.AddDomainEvent(event1);
        userProfile.AddDomainEvent(event2);

        // Act
        userProfile.RemoveDomainEvent(event1);

        // Assert
        userProfile.DomainEvents.Should().HaveCount(1);
        userProfile.DomainEvents.Should().Contain(event2);
        userProfile.DomainEvents.Should().NotContain(event1);
    }

    [Fact]
    public void UserProfile_Should_Support_Version_Control()
    {
        // Arrange
        var userProfile = new UserProfile();
        var initialVersion = userProfile.Version;

        // Act
        userProfile.DisplayName = "Updated Name";
        userProfile.Touch();

        // Assert
        userProfile.Version.Should().Be(initialVersion); // Version only changes on save in EF
        userProfile.UpdatedAt.Should().BeAfter(userProfile.CreatedAt);
    }

    [Fact]
    public void UserProfile_IsNew_Should_Return_True_For_New_Entity()
    {
        // Arrange & Act
        var userProfile = new UserProfile();

        // Assert
        userProfile.IsNew.Should().BeTrue();
        userProfile.Version.Should().Be(0);
    }

    // Test domain event class for testing purposes
    private class TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public int Version { get; } = 1;
        public Guid AggregateId { get; set; } = Guid.NewGuid();
        public string AggregateType { get; } = nameof(TestDomainEvent);
    }
}
