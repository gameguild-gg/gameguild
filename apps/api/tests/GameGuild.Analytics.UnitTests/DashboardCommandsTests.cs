using FluentAssertions;
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
        var dashboard = new Dashboard
        {
            Id = Guid.NewGuid(),
            Title = "Old",
            Slug = "old",
            Widgets =
            {
                new DashboardWidget { Title = "Old widget", Type = WidgetType.Table, SortOrder = 1 }
            }
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(dashboard.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);
        _repository
            .Setup(repository => repository.GetBySlugAsync("new-dashboard", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dashboard?)null);

        var handler = new UpdateDashboardCommandHandler(_repository.Object);

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
}
