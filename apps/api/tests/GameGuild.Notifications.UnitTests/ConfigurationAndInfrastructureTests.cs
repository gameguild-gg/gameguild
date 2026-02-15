using FluentAssertions;
using GameGuild.Notifications.Configuration;
using GameGuild.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Notifications.Tests;

/// <summary>
/// Tests for EF Core configurations and service constructors.
/// </summary>
public class ConfigurationAndInfrastructureTests
{
    private static ModelBuilder CreateModelBuilder() => new(new ConventionSet());

    [Fact]
    public void NotificationConfiguration_ShouldConfigureEntity()
    {
        var mb = CreateModelBuilder();
        new NotificationConfiguration().Configure(mb.Entity<Notification>());
        mb.Model.FindEntityType(typeof(Notification)).Should().NotBeNull();
    }

    [Fact]
    public void NotificationTemplateConfiguration_ShouldConfigureEntity()
    {
        var mb = CreateModelBuilder();
        new NotificationTemplateConfiguration().Configure(mb.Entity<NotificationTemplate>());
        mb.Model.FindEntityType(typeof(NotificationTemplate)).Should().NotBeNull();
    }

    [Fact]
    public void NotificationPreferenceConfiguration_ShouldConfigureEntity()
    {
        var mb = CreateModelBuilder();
        new NotificationPreferenceConfiguration().Configure(mb.Entity<NotificationPreference>());
        mb.Model.FindEntityType(typeof(NotificationPreference)).Should().NotBeNull();
    }

    [Fact]
    public void NotificationDeliveryService_CanBeInstantiated()
    {
        var service = new NotificationDeliveryService(
            Mock.Of<IApplicationDbContext>(),
            Mock.Of<INotificationPreferenceService>(),
            Mock.Of<INotificationTemplateService>(),
            NullLogger<NotificationDeliveryService>.Instance);

        service.Should().NotBeNull();
    }
}
