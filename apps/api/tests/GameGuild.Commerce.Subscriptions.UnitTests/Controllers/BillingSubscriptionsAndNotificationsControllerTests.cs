using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Notifications;
using GameGuild.Notifications.Configuration;
using GameGuild.Notifications.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Controllers;

public sealed class BillingSubscriptionsAndNotificationsControllerTests
{
    [Fact]
    public async Task BillingSubscriptions_ShouldListSubscriptionsThroughBillingRouteContract()
    {
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetPagedSubscriptionsQuery>(query =>
                    query.TenantId == tenantId &&
                    query.Status == SubscriptionStatus.Active &&
                    query.Page == 1 &&
                    query.PageSize == 100),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Subscription>.Empty(100));

        var controller = CreateBillingSubscriptionsController(sender.Object, ActorContext.Anonymous);

        var result = await controller.GetBillingSubscriptions(tenantId, SubscriptionStatus.Active, null, page: 0, pageSize: 500, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task BillingSubscriptions_ShouldDispatchRenewAndCancelCommands()
    {
        var subscriptionId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.Is<ProcessSubscriptionRenewalCommand>(command => command.SubscriptionId == subscriptionId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(service => service.Send(It.Is<CancelSubscriptionCommand>(command => command.SubscriptionId == subscriptionId && command.Reason == CancellationReason.PaymentFailed), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateBillingSubscriptionsController(sender.Object, ActorContext.Anonymous);

        var renew = await controller.RenewBillingSubscription(subscriptionId, CancellationToken.None);
        var cancel = await controller.CancelBillingSubscription(
            subscriptionId,
            new BillingSubscriptionsController.CancelBillingSubscriptionRequest(CancellationReason.PaymentFailed, "card expired", null),
            CancellationToken.None);

        renew.Should().BeOfType<AcceptedResult>();
        cancel.Should().BeOfType<NoContentResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task BillingSubscriptions_ShouldValidateTenantContextAndCreateRequests()
    {
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(It.Is<GetPagedSubscriptionsQuery>(query => query.Page == 2 && query.PageSize == 20 && query.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Subscription>.Empty(20));
        sender.Setup(service => service.Send(It.Is<CreateSubscriptionCommand>(command =>
                command.TenantId == tenantId &&
                command.PlanId == planId &&
                command.CreatedByUserId == userId &&
                command.Amount == 99m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        var tenantController = CreateBillingSubscriptionsController(sender.Object, CreateAuthenticatedActorContext(tenantId));
        var missingTenantController = CreateBillingSubscriptionsController(sender.Object, CreateAuthenticatedActorContext(null));
        var crossTenantController = CreateBillingSubscriptionsController(sender.Object, CreateAuthenticatedActorContext(Guid.NewGuid()));

        var listed = await tenantController.GetBillingSubscriptions(null, null, null, page: 2, pageSize: 0, CancellationToken.None);
        var missingTenant = await missingTenantController.GetBillingSubscriptions(null, null, null, ct: CancellationToken.None);
        var forbidden = await crossTenantController.GetBillingSubscriptions(tenantId, null, null, ct: CancellationToken.None);
        var created = await tenantController.CreateBillingSubscription(
            new BillingSubscriptionsController.CreateBillingSubscriptionRequest(tenantId, planId, userId, BillingCycle.Monthly, 99m),
            CancellationToken.None);
        var emptyTenant = await tenantController.CreateBillingSubscription(
            new BillingSubscriptionsController.CreateBillingSubscriptionRequest(Guid.Empty, planId, userId, BillingCycle.Monthly, 99m),
            CancellationToken.None);
        var emptyPlan = await tenantController.CreateBillingSubscription(
            new BillingSubscriptionsController.CreateBillingSubscriptionRequest(tenantId, Guid.Empty, userId, BillingCycle.Monthly, 99m),
            CancellationToken.None);
        var emptyUser = await tenantController.CreateBillingSubscription(
            new BillingSubscriptionsController.CreateBillingSubscriptionRequest(tenantId, planId, Guid.Empty, BillingCycle.Monthly, 99m),
            CancellationToken.None);
        var negativeAmount = await tenantController.CreateBillingSubscription(
            new BillingSubscriptionsController.CreateBillingSubscriptionRequest(tenantId, planId, userId, BillingCycle.Monthly, -1m),
            CancellationToken.None);

        listed.Should().BeOfType<OkObjectResult>();
        missingTenant.Should().BeOfType<BadRequestObjectResult>();
        forbidden.Should().BeOfType<ForbidResult>();
        created.Should().BeOfType<CreatedAtActionResult>();
        emptyTenant.Should().BeOfType<BadRequestObjectResult>();
        emptyPlan.Should().BeOfType<BadRequestObjectResult>();
        emptyUser.Should().BeOfType<BadRequestObjectResult>();
        negativeAmount.Should().BeOfType<BadRequestObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task SubscriptionNotifications_ShouldQueryAndResendThroughCqrs()
    {
        var notificationId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetSubscriptionNotificationsQuery>(query =>
                    query.TenantId == tenantId &&
                    query.SubscriptionId == subscriptionId &&
                    query.Channel == NotificationChannel.InApp &&
                    query.IsSent == true &&
                    ((query.Page == 1 && query.PageSize == 100) ||
                     (query.Page == 2 && query.PageSize == 20) ||
                     (query.Page == 3 && query.PageSize == 100))),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<SubscriptionNotificationDto>.Empty(100));
        sender
            .Setup(service => service.Send(
                It.Is<ResendSubscriptionNotificationCommand>(command =>
                    command.NotificationId == notificationId &&
                    command.Channel == NotificationChannel.InApp),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionNotificationDto(
                notificationId,
                Guid.NewGuid(),
                tenantId,
                subscriptionId,
                NotificationChannel.InApp.ToString(),
                "Subscription renewed",
                "Renewal notice",
                true,
                DateTime.UtcNow,
                DateTime.UtcNow));

        var controller = new SubscriptionNotificationsController(sender.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var list = await controller.GetSubscriptionNotifications(
            tenantId,
            subscriptionId,
            NotificationChannel.InApp,
            isSent: true,
            page: 0,
            pageSize: 500,
            CancellationToken.None);
        var resend = await controller.ResendSubscriptionNotification(
            notificationId,
            new SubscriptionNotificationsController.ResendSubscriptionNotificationRequest(NotificationChannel.InApp),
            CancellationToken.None);
        var normalPage = await controller.GetSubscriptionNotifications(
            tenantId,
            subscriptionId,
            NotificationChannel.InApp,
            isSent: true,
            page: 2,
            pageSize: 20,
            CancellationToken.None);
        var boundaryPage = await controller.GetSubscriptionNotifications(
            tenantId,
            subscriptionId,
            NotificationChannel.InApp,
            isSent: true,
            page: 3,
            pageSize: 100,
            CancellationToken.None);

        list.Should().BeOfType<OkObjectResult>();
        normalPage.Should().BeOfType<OkObjectResult>();
        boundaryPage.Should().BeOfType<OkObjectResult>();
        resend.Should().BeOfType<AcceptedAtActionResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task SubscriptionNotificationsQueryHandlers_ShouldFilterMapAndResend()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var source = Notification.Create(
            Guid.NewGuid(),
            NotificationType.Billing,
            NotificationChannel.Email,
            "Invoice ready",
            "Your invoice is ready.",
            tenantId,
            priority: NotificationPriority.High,
            referenceEntityId: subscriptionId,
            referenceEntityType: "subscription",
            metadata: "{\"invoice\":\"1\"}");
        source.MarkAsSent();
        var other = Notification.Create(
            Guid.NewGuid(),
            NotificationType.System,
            NotificationChannel.Email,
            "Other",
            "Other",
            tenantId,
            referenceEntityId: subscriptionId,
            referenceEntityType: "subscription");
        var noTenant = Notification.Create(
            Guid.NewGuid(),
            NotificationType.Billing,
            NotificationChannel.InApp,
            "In-app billing",
            "Billing notice",
            referenceEntityId: subscriptionId,
            referenceEntityType: "subscription");
        db.Set<Notification>().AddRange(source, other);
        db.Set<Notification>().Add(noTenant);
        await db.SaveChangesAsync();
        var notificationService = new Mock<INotificationService>();
        var resent = Notification.Create(
            source.RecipientId,
            NotificationType.Billing,
            NotificationChannel.InApp,
            source.Title,
            source.Message,
            tenantId,
            priority: source.Priority,
            referenceEntityId: subscriptionId,
            referenceEntityType: "subscription",
            metadata: source.Metadata);
        notificationService.Setup(service => service.SendAsync(
                source.RecipientId!.Value,
                NotificationType.Billing,
                source.Title,
                source.Message,
                NotificationChannel.InApp,
                tenantId,
                source.ActionUrl,
                source.Priority,
                subscriptionId,
                "subscription",
                source.Metadata,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(resent));

        var page = await new GetSubscriptionNotificationsQueryHandler(db).Handle(
            new GetSubscriptionNotificationsQuery(tenantId, subscriptionId, NotificationChannel.Email, true, Page: 0, PageSize: 500),
            CancellationToken.None);
        var resend = await new ResendSubscriptionNotificationCommandHandler(db, notificationService.Object).Handle(
            new ResendSubscriptionNotificationCommand(source.Id, NotificationChannel.InApp),
            CancellationToken.None);
        var allNotifications = await new GetSubscriptionNotificationsQueryHandler(db).Handle(
            new GetSubscriptionNotificationsQuery(SubscriptionId: subscriptionId, Page: 1, PageSize: 20),
            CancellationToken.None);

        page.TotalCount.Should().Be(1);
        page.PageSize.Should().Be(100);
        page.Items.Single().TenantId.Should().Be(tenantId);
        page.Items.Single().Channel.Should().Be(NotificationChannel.Email.ToString());
        allNotifications.Items.Should().Contain(item => item.TenantId == null);
        resend.Channel.Should().Be(NotificationChannel.InApp.ToString());
    }

    [Fact]
    public async Task SubscriptionNotificationsResend_ShouldThrowForMissingOrFailedDelivery()
    {
        await using var db = CreateDbContext();
        var source = Notification.Create(
            Guid.NewGuid(),
            NotificationType.Billing,
            NotificationChannel.Email,
            "Invoice ready",
            "Your invoice is ready.",
            referenceEntityId: Guid.NewGuid(),
            referenceEntityType: "subscription");
        db.Set<Notification>().Add(source);
        await db.SaveChangesAsync();
        var notificationService = new Mock<INotificationService>();
        notificationService.Setup(service => service.SendAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationChannel>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Notification>(Error.Problem("notifications.failed", "delivery failed")));
        var handler = new ResendSubscriptionNotificationCommandHandler(db, notificationService.Object);

        await FluentActions.Awaiting(() => handler.Handle(new ResendSubscriptionNotificationCommand(Guid.NewGuid()), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Subscription notification * was not found.");
        await FluentActions.Awaiting(() => handler.Handle(new ResendSubscriptionNotificationCommand(source.Id), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("delivery failed");
    }

    [Fact]
    public async Task ClientsController_ShouldDispatchClientAndModuleRoutes()
    {
        var sender = new Mock<ISender>();
        var clientId = Guid.NewGuid();
        sender.Setup(service => service.Send(It.Is<CreateTenantCommand>(command => command.Name == "Acme" && command.Slug == "acme"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientId);
        sender.Setup(service => service.Send(It.Is<CreateTenantCommand>(command => command.Name == "No Fiscal" && command.Slug == "no-fiscal"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        sender.Setup(service => service.Send(It.IsAny<UpdateTenantMetadataCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(service => service.Send(It.Is<GetTenantsPageQuery>(query => query.Page == 1 && query.PageSize == 500 && query.IsActive == true && query.IsArchived == false), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Tenant>.Empty(500));
        sender.Setup(service => service.Send(It.Is<GetTenantsPageQuery>(query => query.Page == 2 && query.PageSize == 500 && query.IsActive == true && query.IsArchived == false), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Tenant>.Empty(500));
        sender.Setup(service => service.Send(It.Is<GetTenantByIdQuery>(query => query.TenantId == clientId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        sender.Setup(service => service.Send(It.Is<UpdateTenantCommand>(command => command.TenantId == clientId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(service => service.Send(It.Is<ArchiveTenantCommand>(command => command.TenantId == clientId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchiveTenantResponse { Success = true, TenantId = clientId });
        sender.Setup(service => service.Send(It.Is<GetPagedSubscriptionsQuery>(query => query.TenantId == clientId && query.Page == 1 && query.PageSize == 100), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Subscription>.Empty(100));
        sender.Setup(service => service.Send(It.Is<GetTenantFeatureFlagsQuery>(query => query.TenantId == clientId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dictionary<string, bool>?)null);
        sender.Setup(service => service.Send(It.Is<UpdateTenantFeatureFlagsCommand>(command => command.TenantId == clientId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = new ClientsController(sender.Object);

        var created = await controller.CreateClient(
            new CreateClientRequest("Acme", "acme", "admin@example.com", "Client", "11.222.333/0001-81", " tax-1 ", new Dictionary<string, object?> { ["segment"] = "enterprise" }),
            CancellationToken.None);
        var invalid = await controller.CreateClient(
            new CreateClientRequest("Acme", "acme", "admin@example.com", Cnpj: "11.111.111/1111-11"),
            CancellationToken.None);
        var invalidLength = await controller.CreateClient(
            new CreateClientRequest("Acme", "acme", "admin@example.com", Cnpj: "123"),
            CancellationToken.None);
        var invalidFirstDigit = await controller.CreateClient(
            new CreateClientRequest("Acme", "acme", "admin@example.com", Cnpj: "11.222.333/0001-11"),
            CancellationToken.None);
        var invalidSecondDigit = await controller.CreateClient(
            new CreateClientRequest("Acme", "acme", "admin@example.com", Cnpj: "11.222.333/0001-82"),
            CancellationToken.None);
        var noFiscal = await controller.CreateClient(
            new CreateClientRequest("No Fiscal", "no-fiscal", "admin@example.com"),
            CancellationToken.None);
        var listed = await controller.GetClients(page: 0, pageSize: 900, status: " active ", searchTerm: "acme", CancellationToken.None);
        var boundaryList = await controller.GetClients(page: 2, pageSize: 500, status: "active", searchTerm: "acme", CancellationToken.None);
        var missing = await controller.GetClientById(clientId, CancellationToken.None);
        var updated = await controller.UpdateClientById(clientId, new UpdateTenantRequest("Acme 2", "Updated"), CancellationToken.None);
        var deleted = await controller.DeleteClientById(clientId, new ArchiveRequest("Closed"), CancellationToken.None);
        var modules = await controller.GetClientModules(clientId, page: 0, pageSize: 500, SubscriptionStatus.Active, CancellationToken.None);
        var updatedModules = await controller.UpdateClientModules(clientId, new UpdateTenantFeatureFlagsRequest(new Dictionary<string, bool> { ["ai"] = true }), CancellationToken.None);

        created.Should().BeOfType<CreatedAtActionResult>();
        invalid.Should().BeOfType<BadRequestObjectResult>();
        invalidLength.Should().BeOfType<BadRequestObjectResult>();
        invalidFirstDigit.Should().BeOfType<BadRequestObjectResult>();
        invalidSecondDigit.Should().BeOfType<BadRequestObjectResult>();
        noFiscal.Should().BeOfType<CreatedAtActionResult>();
        listed.Should().BeOfType<OkObjectResult>();
        boundaryList.Should().BeOfType<OkObjectResult>();
        missing.Should().BeOfType<NotFoundResult>();
        updated.Should().BeOfType<NoContentResult>();
        deleted.Should().BeOfType<NoContentResult>();
        var okModules = modules.Should().BeOfType<OkObjectResult>().Subject;
        okModules.Value.Should().BeOfType<ClientModulesResponse>();
        updatedModules.Should().BeOfType<NoContentResult>();
        sender.VerifyAll();
    }

    [Fact]
    public void ClientsControllerPrivateCnpjDigitHelper_ShouldCoverBothRemainderBranches()
    {
        var method = typeof(ClientsController)
            .GetMethod("CalculateCnpjDigit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var zeroDigit = method!.Invoke(null, ["00000000000000", 12]);
        var nonZeroDigit = method.Invoke(null, ["11222333000181", 12]);

        zeroDigit.Should().Be(0);
        nonZeroDigit.Should().Be(8);
    }

    [Fact]
    public void PrivatePagingHelpers_ShouldLeaveNormalPageSizesUncapped()
    {
        var clientMethod = typeof(ClientsController)
            .GetMethod("NormalizePaging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var notificationMethod = typeof(SubscriptionNotificationsController)
            .GetMethod("NormalizePaging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        object?[] clientArgs = [1, 20, 100];
        object?[] notificationArgs = [1, 20];

        clientMethod!.Invoke(null, clientArgs);
        notificationMethod!.Invoke(null, notificationArgs);

        clientArgs[0].Should().Be(1);
        clientArgs[1].Should().Be(20);
        notificationArgs[0].Should().Be(1);
        notificationArgs[1].Should().Be(20);
    }

    [Theory]
    [InlineData("inactive", false, false)]
    [InlineData("archived", null, true)]
    [InlineData("unknown", null, null)]
    [InlineData(null, null, null)]
    public async Task ClientsController_ShouldNormalizeStatusFilters(string? status, bool? expectedActive, bool? expectedArchived)
    {
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(
                It.Is<GetTenantsPageQuery>(query => query.IsActive == expectedActive && query.IsArchived == expectedArchived),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Tenant>.Empty(20));
        var controller = new ClientsController(sender.Object);

        var result = await controller.GetClients(status: status, ct: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    [Fact]
    public async Task SubscriptionReports_ShouldDispatchChurnReportQuery()
    {
        var tenantId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(
                It.Is<GetSubscriptionChurnReportQuery>(query =>
                    query.TenantId == tenantId &&
                    query.StartDate == start &&
                    query.EndDate == end),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionChurnReportDto(
                tenantId,
                start,
                end,
                TotalSubscriptions: 10,
                ActiveSubscriptions: 8,
                CancelledInPeriod: 2,
                ChurnRate: 20m,
                RetentionRate: 80m,
                MonthlyRecurringRevenue: 1000m,
                GeneratedAt: DateTime.UtcNow,
                StatusBreakdown: new Dictionary<string, int> { ["Active"] = 8, ["Cancelled"] = 2 }));

        var controller = new SubscriptionReportsController(sender.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetChurnReport(tenantId, start, end, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    private static BillingSubscriptionsController CreateBillingSubscriptionsController(ISender sender, ActorContext actorContext)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(value => value.ActorContext).Returns(actorContext);

        return new BillingSubscriptionsController(sender, accessor.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static ActorContext CreateAuthenticatedActorContext(Guid? tenantId)
        => new()
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Test",
            IsAuthenticated = true
        };

    private static SubscriptionsNotificationTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SubscriptionsNotificationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SubscriptionsNotificationTestDbContext(options);
    }

    private sealed class SubscriptionsNotificationTestDbContext(DbContextOptions<SubscriptionsNotificationTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
