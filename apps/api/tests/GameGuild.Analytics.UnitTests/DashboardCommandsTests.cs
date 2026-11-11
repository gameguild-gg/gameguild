using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Analytics.UnitTests;

public sealed class DashboardCommandsTests
{
    private readonly Mock<IDashboardRepository> _repository = new();

    [Fact]
    public async Task CreateDashboardCommand_ShouldPersistDashboardWithWidgets()
    {
        Dashboard? captured = null;
        _repository
            .Setup(repository => repository.GetBySlugAsync("ops-dashboard", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dashboard?)null);
        _repository
            .Setup(repository => repository.AddAsync(It.IsAny<Dashboard>(), It.IsAny<CancellationToken>()))
            .Callback<Dashboard, CancellationToken>((dashboard, _) => captured = dashboard)
            .ReturnsAsync((Dashboard dashboard, CancellationToken _) => dashboard);

        var handler = new CreateDashboardCommandHandler(_repository.Object);
        var tenantId = Guid.NewGuid();

        var result = await handler.Handle(new CreateDashboardCommand(new CreateDashboardRequest(
            " Operations ",
            " OPS-Dashboard ",
            " Daily metrics ",
            true,
            tenantId,
            [
                new DashboardWidgetRequest("MRR", WidgetType.Counter, 2, """{"kpi":"mrr"}"""),
                new DashboardWidgetRequest("SLA", WidgetType.Gauge, 1)
            ])), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Title.Should().Be("Operations");
        captured.Slug.Should().Be("ops-dashboard");
        captured.TenantId.Should().Be(tenantId);
        captured.Widgets.Should().HaveCount(2);
        captured.Widgets.Should().OnlyContain(widget => widget.TenantId == tenantId);
        result.Widgets.Select(widget => widget.SortOrder).Should().Equal(1, 2);
    }

    [Fact]
    public async Task UpdateDashboardCommand_ShouldReplaceWidgets_WhenWidgetsAreProvided()
    {
        var tenantId = Guid.NewGuid();
        var dashboard = new Dashboard
        {
            Id = Guid.NewGuid(),
            Title = "Old",
            Slug = "old",
            TenantId = tenantId,
            Widgets =
            {
                new DashboardWidget { Title = "Old widget", Type = WidgetType.Table, SortOrder = 1 }
            }
        };

        _repository
            .Setup(repository => repository.GetByIdForTenantAsync(dashboard.Id, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);
        _repository
            .Setup(repository => repository.GetBySlugAsync("new-dashboard", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dashboard?)null);

        var handler = new UpdateDashboardCommandHandler(_repository.Object, ActorAccessor(tenantId).Object);

        var result = await handler.Handle(new UpdateDashboardCommand(
            dashboard.Id,
            new UpdateDashboardRequest(
                "New",
                "new-dashboard",
                Widgets:
                [
                    new DashboardWidgetRequest("New widget", WidgetType.TimeSeries, 1)
                ])), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Title.Should().Be("New");
        result.Widgets.Should().ContainSingle(widget => widget.Title == "New widget" && widget.Type == WidgetType.TimeSeries);
        _repository.Verify(repository => repository.UpdateAsync(dashboard, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Issue #1250 N10: the id-addressed dashboard read/write must scope to the
    ///     authenticated actor's tenant — another tenant's dashboard is not found.
    /// </summary>
    [Fact]
    public async Task DashboardByIdAndUpdate_WhenDashboardBelongsToAnotherTenant_ReturnNotFound()
    {
        var actorTenant = Guid.NewGuid();
        var foreignTenant = Guid.NewGuid();
        var foreignDashboard = new Dashboard { Id = Guid.NewGuid(), Title = "Foreign", Slug = "foreign", TenantId = foreignTenant };

        _repository
            .Setup(repository => repository.GetByIdForTenantAsync(foreignDashboard.Id, actorTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dashboard?)null);

        var byId = await new GetDashboardByIdQueryHandler(_repository.Object, ActorAccessor(actorTenant).Object)
            .Handle(new GetDashboardByIdQuery(foreignDashboard.Id), CancellationToken.None);
        var updated = await new UpdateDashboardCommandHandler(_repository.Object, ActorAccessor(actorTenant).Object)
            .Handle(new UpdateDashboardCommand(foreignDashboard.Id, new UpdateDashboardRequest("Hijacked", "hijacked")), CancellationToken.None);

        byId.Should().BeNull();
        updated.Should().BeNull();
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Dashboard>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDashboardsQuery_ShouldIgnoreCallerAssertedTenantForNonAdminActors()
    {
        var actorTenant = Guid.NewGuid();
        var assertedTenant = Guid.NewGuid();
        var own = new Dashboard { Id = Guid.NewGuid(), Title = "Own", Slug = "own", TenantId = actorTenant };
        _repository
            .Setup(repository => repository.GetAllAsync(actorTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync([own]);

        var listed = await new GetDashboardsQueryHandler(_repository.Object, ActorAccessor(actorTenant).Object)
            .Handle(new GetDashboardsQuery(assertedTenant), CancellationToken.None);

        listed.Should().ContainSingle().Which.Title.Should().Be("Own");
        _repository.Verify(repository => repository.GetAllAsync(It.Is<Guid?>(tenant => tenant != actorTenant), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDashboardsQuery_ForTenantlessActor_ReturnsNothingFailClosed()
    {
        var listed = await new GetDashboardsQueryHandler(_repository.Object, ActorAccessor(tenantId: null).Object)
            .Handle(new GetDashboardsQuery(), CancellationToken.None);

        listed.Should().BeEmpty();
        _repository.Verify(repository => repository.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDashboardsQuery_SystemAdminMayScopeToRequestedTenant()
    {
        var requestedTenant = Guid.NewGuid();
        var requested = new Dashboard { Id = Guid.NewGuid(), Title = "Requested", Slug = "requested", TenantId = requestedTenant };
        _repository
            .Setup(repository => repository.GetAllAsync(requestedTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync([requested]);
        var admin = new Mock<IActorContextAccessor>();
        admin.SetupGet(value => value.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = Guid.NewGuid(),
            Roles = new HashSet<string> { "SystemAdmin" },
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        });

        var listed = await new GetDashboardsQueryHandler(_repository.Object, admin.Object)
            .Handle(new GetDashboardsQuery(requestedTenant), CancellationToken.None);

        listed.Should().ContainSingle().Which.Title.Should().Be("Requested");
    }

    internal static Mock<IActorContextAccessor> ActorAccessor(Guid? tenantId, Guid? userId = null)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(value => value.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = (userId ?? Guid.NewGuid()).ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            AuthScheme = "Bearer",
            IsAuthenticated = true
        });
        return accessor;
    }
}
