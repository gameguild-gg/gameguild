using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GameGuild.API.UnitTests.Database;

public sealed class ApplicationDbContextDomainEventTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldPublishAndClearDomainEventsAfterPersistence()
    {
        var publisher = new Mock<IPublisher>(MockBehavior.Strict);
        publisher
            .Setup(instance => instance.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options, publisher.Object);
        var tenant = new Tenant
        {
            Name = "Domain event tenant",
            Slug = "domain-event-tenant",
            AdminEmail = "admin@example.com"
        };
        context.Add(tenant);
        tenant.Deactivate();

        await context.SaveChangesAsync();

        publisher.Verify(
            instance => instance.Publish(
                It.Is<IDomainEvent>(domainEvent => domainEvent is TenantDeactivatedEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
        tenant.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldResolvePublisherFromContextServices()
    {
        var publisher = new Mock<IPublisher>(MockBehavior.Strict);
        publisher.Setup(instance => instance.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddEntityFrameworkInMemoryDatabase();
        services.AddSingleton(publisher.Object);
        await using var provider = services.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(provider)
            .Options;
        await using var context = new ApplicationDbContext(options);
        var tenant = new Tenant
        {
            Name = "Context publisher",
            Slug = "context-publisher",
            AdminEmail = "admin@example.com"
        };
        context.Add(tenant);
        tenant.Deactivate();

        await context.SaveChangesAsync();

        publisher.Verify(instance => instance.Publish(
            It.Is<IDomainEvent>(domainEvent => domainEvent is TenantDeactivatedEvent),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
