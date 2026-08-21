using GameGuild.Notifications.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GameGuild.Notifications.UnitTests.Configuration;

public class EmailDeliveryConfigurationTests
{
    [Fact]
    public void EmailDeliveryEvent_Model_Should_Have_Indexes_Lengths_And_Query_Filter()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EmailDeliveryEvent));

        entityType.Should().NotBeNull();
        entityType!.GetDeclaredQueryFilters().Should().NotBeEmpty();

        entityType.FindProperty(nameof(EmailDeliveryEvent.ProviderMessageId))!.GetMaxLength().Should().Be(100);
        entityType.FindProperty(nameof(EmailDeliveryEvent.RecipientEmail))!.GetMaxLength().Should().Be(320);
        entityType.FindProperty(nameof(EmailDeliveryEvent.EventType))!.GetMaxLength().Should().Be(20);
        entityType.FindProperty(nameof(EmailDeliveryEvent.BounceType))!.GetMaxLength().Should().Be(30);
        entityType.FindProperty(nameof(EmailDeliveryEvent.DiagnosticCode))!.GetMaxLength().Should().Be(200);
        entityType.FindProperty(nameof(EmailDeliveryEvent.SnsMessageId))!.GetMaxLength().Should().Be(100);

        // jsonb payload: relational column type, no varchar MaxLength
        var payload = entityType.FindProperty(nameof(EmailDeliveryEvent.Payload))!;
        payload.GetMaxLength().Should().BeNull();
        ((string?)payload["Relational:ColumnType"]).Should().Be("jsonb");

        GetIndex(entityType, nameof(EmailDeliveryEvent.ProviderMessageId)).IsUnique.Should().BeFalse();
        GetIndex(entityType, nameof(EmailDeliveryEvent.SnsMessageId)).IsUnique.Should().BeTrue();
    }

    [Fact]
    public void EmailSuppression_Model_Should_Have_Unique_Email_Index_Reason_Length_And_Query_Filter()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(EmailSuppression));

        entityType.Should().NotBeNull();
        entityType!.GetDeclaredQueryFilters().Should().NotBeEmpty();

        entityType.FindProperty(nameof(EmailSuppression.EmailAddress))!.GetMaxLength().Should().Be(320);
        entityType.FindProperty(nameof(EmailSuppression.Reason))!.GetMaxLength().Should().Be(20);
        entityType.FindProperty(nameof(EmailSuppression.BounceType))!.GetMaxLength().Should().Be(30);

        GetIndex(entityType, nameof(EmailSuppression.EmailAddress)).IsUnique.Should().BeTrue();
    }

    [Fact]
    public void Notification_Model_Should_Have_ProviderMessageId_Property_And_Index()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Notification));

        entityType.Should().NotBeNull();
        entityType!.FindProperty(nameof(Notification.ProviderMessageId))!.GetMaxLength().Should().Be(100);
        GetIndex(entityType!, nameof(Notification.ProviderMessageId)).IsUnique.Should().BeFalse();
    }

    private static IIndex GetIndex(IEntityType entityType, string propertyName)
    {
        var property = entityType.FindProperty(propertyName)
            ?? throw new InvalidOperationException($"Property {propertyName} not found on {entityType.DisplayName()}");
        return entityType.GetIndexes().Single(i => i.Properties.Count == 1 && i.Properties[0] == property);
    }

    private static NotificationsTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NotificationsTestDbContext(options);
    }
}
