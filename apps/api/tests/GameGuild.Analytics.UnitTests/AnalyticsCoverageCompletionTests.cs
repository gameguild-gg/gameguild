using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Analytics.UnitTests;

public sealed class AnalyticsCoverageCompletionTests
{
    [Fact]
    public async Task AnalyticsCommandHandlers_ShouldDelegateToService()
    {
        var service = new Mock<IAnalyticsService>();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);
        var tracked = new AnalyticsEventDto(Guid.NewGuid(), "login", "{}", userId, "session", start);
        var series = new List<TimeSeriesDataPointDto> { new(start, 2) };
        var kpi = new KpiResultDto("logins", 2, start, end, end);
        var funnel = new FunnelAnalysisResultDto([new FunnelStepDto("visit", 10, 0)], start, end, 10);

        service.Setup(current => current.TrackEventAsync("login", "{}", userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracked);
        service.Setup(current => current.GetTimeSeriesAsync("login", start, end, TimeSeriesGranularity.Day, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        service.Setup(current => current.CalculateKpiAsync("logins", start, end, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(kpi);
        service.Setup(current => current.AnalyzeFunnelAsync(It.Is<string[]>(steps => steps.SequenceEqual(new[] { "visit", "signup" })), start, end, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(funnel);

        (await new TrackAnalyticsEventCommandHandler(service.Object).Handle(new TrackAnalyticsEventCommand("login", "{}", userId, tenantId), CancellationToken.None))
            .Should().BeSameAs(tracked);
        (await new GetTimeSeriesQueryHandler(service.Object).Handle(new GetTimeSeriesQuery("login", start, end, TimeSeriesGranularity.Day, tenantId), CancellationToken.None))
            .Should().BeSameAs(series);
        (await new CalculateKpiQueryHandler(service.Object).Handle(new CalculateKpiQuery("logins", start, end, tenantId), CancellationToken.None))
            .Should().BeSameAs(kpi);
        (await new AnalyzeFunnelQueryHandler(service.Object).Handle(new AnalyzeFunnelQuery(["visit", "signup"], start, end, tenantId), CancellationToken.None))
            .Should().BeSameAs(funnel);
    }

    [Fact]
    public async Task DashboardCommands_ShouldCoverValidationDuplicateNullWidgetAndMappingBranches()
    {
        var repository = new Mock<IDashboardRepository>();
        var existing = new Dashboard { Id = Guid.NewGuid(), Slug = "existing", Title = "Existing" };
        repository.Setup(current => current.GetBySlugAsync("duplicate", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        repository.Setup(current => current.GetBySlugAsync("empty-widgets", It.IsAny<CancellationToken>())).ReturnsAsync((Dashboard?)null);
        repository.Setup(current => current.AddAsync(It.IsAny<Dashboard>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dashboard dashboard, CancellationToken _) => dashboard);
        var create = new CreateDashboardCommandHandler(repository.Object);

        await create.Invoking(current => current.Handle(new CreateDashboardCommand(new CreateDashboardRequest(" ", "slug")), CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
        await create.Invoking(current => current.Handle(new CreateDashboardCommand(new CreateDashboardRequest("Title", " ")), CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
        await create.Invoking(current => current.Handle(new CreateDashboardCommand(new CreateDashboardRequest("Title", "duplicate")), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        await create.Invoking(current => current.Handle(new CreateDashboardCommand(new CreateDashboardRequest("Title", "empty-widgets", Widgets:
            [new DashboardWidgetRequest(" ", WidgetType.Counter, 0)])), CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();

        var created = await create.Handle(new CreateDashboardCommand(new CreateDashboardRequest("Title", "empty-widgets", Widgets: null)), CancellationToken.None);
        created.Widgets.Should().BeEmpty();

        var dashboard = new Dashboard
        {
            Id = Guid.NewGuid(),
            Title = "Current",
            Slug = "current",
            Widgets =
            {
                new DashboardWidget { Id = Guid.NewGuid(), Title = "Deleted", SortOrder = 0, Type = WidgetType.Table, DeletedAt = DateTime.UtcNow },
                new DashboardWidget { Id = Guid.NewGuid(), Title = "Visible", SortOrder = 1, Type = WidgetType.Gauge }
            }
        };
        repository.Setup(current => current.GetByIdAsync(dashboard.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dashboard);
        repository.Setup(current => current.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>())).ReturnsAsync((Dashboard?)null);
        repository.Setup(current => current.GetBySlugAsync("current", It.IsAny<CancellationToken>())).ReturnsAsync(dashboard);
        repository.Setup(current => current.GetBySlugAsync("conflict", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var update = new UpdateDashboardCommandHandler(repository.Object);

        (await update.Handle(new UpdateDashboardCommand(Guid.Empty, new UpdateDashboardRequest("Title", "current")), CancellationToken.None))
            .Should().BeNull();
        await update.Invoking(current => current.Handle(new UpdateDashboardCommand(dashboard.Id, new UpdateDashboardRequest("Title", "conflict")), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        var updated = await update.Handle(new UpdateDashboardCommand(dashboard.Id, new UpdateDashboardRequest(" Updated ", " current ", " ", Widgets: null)), CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.Description.Should().BeNull();
        updated.Widgets.Should().ContainSingle().Which.Title.Should().Be("Visible");
    }

    [Fact]
    public async Task DashboardQueryHandlers_ShouldSortAndMapResults()
    {
        var tenantId = Guid.NewGuid();
        var dashboards = new List<Dashboard>
        {
            new() { Id = Guid.NewGuid(), Title = "B", Slug = "b", IsDefault = false },
            new() { Id = Guid.NewGuid(), Title = "A", Slug = "a", IsDefault = true }
        };
        var repository = new Mock<IDashboardRepository>();
        repository.Setup(current => current.GetAllAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(dashboards);
        repository.Setup(current => current.GetByIdAsync(dashboards[0].Id, It.IsAny<CancellationToken>())).ReturnsAsync(dashboards[0]);
        repository.Setup(current => current.GetByIdAsync(Guid.Empty, It.IsAny<CancellationToken>())).ReturnsAsync((Dashboard?)null);

        var all = await new GetDashboardsQueryHandler(repository.Object).Handle(new GetDashboardsQuery(tenantId), CancellationToken.None);
        var one = await new GetDashboardByIdQueryHandler(repository.Object).Handle(new GetDashboardByIdQuery(dashboards[0].Id), CancellationToken.None);
        var missing = await new GetDashboardByIdQueryHandler(repository.Object).Handle(new GetDashboardByIdQuery(Guid.Empty), CancellationToken.None);

        all.Select(current => current.Title).Should().Equal("A", "B");
        one.Should().NotBeNull();
        one!.Slug.Should().Be("b");
        missing.Should().BeNull();
    }

    [Fact]
    public void AnalyticsModuleAndEfConfigurations_ShouldRegisterAndConfigureExpectedModel()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{AnalyticsWarehouseOptions.SectionName}:Enabled"] = "true",
                [$"{AnalyticsWarehouseOptions.SectionName}:DefaultLookbackDays"] = "15"
            })
            .Build();

        var returned = services.AddAnalyticsModule(configuration);
        var modelBuilder = new ModelBuilder();
        new AnalyticsModelConfiguration().Configure(modelBuilder);
        var model = modelBuilder.Model;

        returned.Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAnalyticsEventRepository) && descriptor.ImplementationType == typeof(AnalyticsEventRepository));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IKpiDefinitionRepository) && descriptor.ImplementationType == typeof(KpiDefinitionRepository));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IDashboardRepository) && descriptor.ImplementationType == typeof(DashboardRepository));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAnalyticsService) && descriptor.ImplementationType == typeof(AnalyticsService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAnalyticsDataWarehouseService) && descriptor.ImplementationType == typeof(AnalyticsDataWarehouseService));

        var eventType = model.FindEntityType(typeof(AnalyticsEvent));
        var kpiType = model.FindEntityType(typeof(KpiDefinition));
        var dashboardType = model.FindEntityType(typeof(Dashboard));
        var widgetType = model.FindEntityType(typeof(DashboardWidget));

        eventType.Should().NotBeNull();
        eventType!.FindProperty(nameof(AnalyticsEvent.EventName))!.GetMaxLength().Should().Be(200);
        eventType.FindProperty(nameof(AnalyticsEvent.Properties))!.GetColumnType().Should().Be("jsonb");
        eventType.GetIndexes().Should().Contain(index => index.Properties.Any(property => property.Name == nameof(AnalyticsEvent.UserId)));
        kpiType!.FindProperty(nameof(KpiDefinition.Name))!.GetMaxLength().Should().Be(200);
        dashboardType!.FindProperty(nameof(Dashboard.Title))!.GetMaxLength().Should().Be(200);
        dashboardType.GetNavigations().Should().Contain(navigation => navigation.Name == nameof(Dashboard.Widgets));
        widgetType!.FindProperty(nameof(DashboardWidget.Configuration))!.GetColumnType().Should().Be("jsonb");
        widgetType.FindProperty(nameof(DashboardWidget.Type))!.GetMaxLength().Should().Be(50);
    }

    [Fact]
    public async Task AnalyticsDataWarehouseService_ShouldBuildCsvAndMapFactsThroughAllBranches()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var localStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var end = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        db.Set<AnalyticsEvent>().AddRange(
            new AnalyticsEvent
            {
                Id = Guid.NewGuid(),
                EventName = "warehouse.valid",
                TenantId = tenantId,
                Timestamp = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
                Properties = "{\"runId\":\"" + runId + "\",\"metric\":\"payments\",\"count\":2,\"amountUsd\":42.5,\"dimensions\":{\"status\":\"paid\"}}"
            },
            new AnalyticsEvent
            {
                Id = Guid.NewGuid(),
                EventName = "warehouse.invalid",
                TenantId = tenantId,
                Timestamp = new DateTime(2026, 1, 14, 12, 0, 0, DateTimeKind.Utc),
                Properties = "{not-json"
            },
            new AnalyticsEvent
            {
                Id = Guid.NewGuid(),
                EventName = "warehouse.empty",
                TenantId = tenantId,
                Timestamp = new DateTime(2026, 1, 13, 12, 0, 0, DateTimeKind.Utc),
                Properties = " "
            },
            new AnalyticsEvent
            {
                Id = Guid.NewGuid(),
                EventName = "other.event",
                TenantId = tenantId,
                Timestamp = new DateTime(2026, 1, 13, 12, 0, 0, DateTimeKind.Utc)
            });
        await db.SaveChangesAsync();
        var service = new AnalyticsDataWarehouseService(
            db,
            Options.Create(new AnalyticsWarehouseOptions { Enabled = true, DefaultLookbackDays = 10 }));

        var allFacts = await service.GetFactsAsync(new AnalyticsWarehouseExportRequest(localStart, end, tenantId, Take: 5001));
        var validFacts = await service.GetFactsAsync(new AnalyticsWarehouseExportRequest(localStart, end, tenantId, " warehouse.valid ", 1));
        var csv = service.BuildCsv([
            new AnalyticsWarehouseFactDto(
                Guid.NewGuid(),
                tenantId,
                "warehouse,quoted",
                end,
                runId,
                "line\nbreak\"metric",
                null,
                null,
                new Dictionary<string, string?> { ["quote"] = "a,b" })
        ]);
        var csvVariants = service.BuildCsv([
            new AnalyticsWarehouseFactDto(Guid.NewGuid(), tenantId, "quote\"only", end, runId, "plain", 5, 6.5m, new Dictionary<string, string?>()),
            new AnalyticsWarehouseFactDto(Guid.NewGuid(), tenantId, "line\nonly", end, runId, "plain", 1, 2m, new Dictionary<string, string?>()),
            new AnalyticsWarehouseFactDto(Guid.NewGuid(), tenantId, "carriage\ronly", end, runId, "plain", 3, 4m, new Dictionary<string, string?>()),
            new AnalyticsWarehouseFactDto(Guid.NewGuid(), tenantId, "plain", end, runId, "plain", 7, 8m, new Dictionary<string, string?>())
        ]);
        var defaultWindowFacts = await service.GetFactsAsync(new AnalyticsWarehouseExportRequest(TenantId: tenantId, Take: 1));
        var disabled = new AnalyticsDataWarehouseService(
            db,
            Options.Create(new AnalyticsWarehouseOptions { Enabled = false }));

        allFacts.Should().HaveCount(3);
        allFacts.Should().Contain(fact => fact.FactName == "warehouse.valid" && fact.RunId == runId && fact.Metric == "payments" && fact.Count == 2 && fact.AmountUsd == 42.5m);
        allFacts.Should().Contain(fact => fact.FactName == "warehouse.invalid" && fact.RunId == null && fact.Metric == string.Empty);
        allFacts.Should().Contain(fact => fact.FactName == "warehouse.empty" && fact.Dimensions.Count == 0);
        validFacts.Should().ContainSingle().Which.FactName.Should().Be("warehouse.valid");
        csv.Should().Contain("\"warehouse,quoted\"");
        csv.Should().Contain("\"line\nbreak\"\"metric\"");
        csvVariants.Should().Contain("\"quote\"\"only\"");
        csvVariants.Should().Contain("\"line\nonly\"");
        csvVariants.Should().Contain("\"carriage\ronly\"");
        csvVariants.Should().Contain(",5,6.5,");
        defaultWindowFacts.Should().BeEmpty();
        await disabled.Invoking(current => current.MaterializeAsync(new AnalyticsWarehouseRunRequest()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void AnalyticsDataWarehousePrivateBuildFact_ShouldCreateSerializableEvent()
    {
        var runId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var timestamp = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        var method = typeof(AnalyticsDataWarehouseService).GetMethod(
            "BuildFact",
            BindingFlags.NonPublic | BindingFlags.Static);

        var evt = (AnalyticsEvent)method!.Invoke(null, [
            "warehouse.test",
            runId,
            tenantId,
            timestamp,
            "metric",
            7,
            10.5m,
            new Dictionary<string, string?> { ["kind"] = "test" }
        ])!;

        evt.EventName.Should().Be("warehouse.test");
        evt.TenantId.Should().Be(tenantId);
        evt.Timestamp.Should().Be(timestamp);
        evt.Environment.Should().Be("warehouse");
        evt.Properties.Should().Contain("metric");
    }

    [Fact]
    public async Task AnalyticsRepositoriesAndService_ShouldOperateAgainstDbContext()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventRepository = new AnalyticsEventRepository(db);
        var kpiRepository = new KpiDefinitionRepository(db);
        var dashboardRepository = new DashboardRepository(db);
        var service = new AnalyticsService(eventRepository, kpiRepository);
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);

        await eventRepository.AddAsync(new AnalyticsEvent
        {
            EventName = "login",
            UserId = userId,
            TenantId = tenantId,
            Timestamp = new DateTime(2026, 1, 10)
        });
        await eventRepository.AddRangeAsync([
            new AnalyticsEvent { EventName = "signup", UserId = userId, TenantId = tenantId, Timestamp = new DateTime(2026, 1, 11) },
            new AnalyticsEvent { EventName = "signup", UserId = Guid.NewGuid(), TenantId = tenantId, Timestamp = new DateTime(2026, 1, 12), DeletedAt = DateTime.UtcNow }
        ]);
        var kpi = await kpiRepository.AddAsync(new KpiDefinition { Name = "signups", EventName = "signup" });
        var inactive = await kpiRepository.AddAsync(new KpiDefinition { Name = "inactive", EventName = "inactive", IsActive = false });
        inactive.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var dashboard = await dashboardRepository.AddAsync(new Dashboard
        {
            Title = "Ops",
            Slug = "ops",
            Widgets = { new DashboardWidget { Title = "Widget", Type = WidgetType.Counter, SortOrder = 1 } }
        });

        var tracked = await service.TrackEventAsync("tracked", "{}", userId, tenantId);
        var byName = await eventRepository.GetByEventNameAsync("signup", start, end, tenantId);
        var byUser = await eventRepository.GetByUserIdAsync(userId, start, end);
        var count = await eventRepository.CountAsync("signup", start, end, tenantId);
        var activeKpis = await kpiRepository.GetAllActiveAsync();
        kpi.Description = "Updated";
        await kpiRepository.UpdateAsync(kpi);
        var byKpiName = await kpiRepository.GetByNameAsync("signups");
        var dashboards = await dashboardRepository.GetAllAsync(null);
        var tenantDashboards = await dashboardRepository.GetAllAsync(tenantId);
        var byId = await dashboardRepository.GetByIdAsync(dashboard.Id);
        var bySlug = await dashboardRepository.GetBySlugAsync("ops");
        dashboard.Title = "Ops Updated";
        await dashboardRepository.UpdateAsync(dashboard);

        tracked.EventName.Should().Be("tracked");
        byName.Should().ContainSingle().Which.EventName.Should().Be("signup");
        byUser.Should().HaveCountGreaterThanOrEqualTo(2);
        count.Should().Be(1);
        activeKpis.Should().ContainSingle().Which.Name.Should().Be("signups");
        byKpiName.Should().BeSameAs(kpi);
        dashboards.Should().ContainSingle().Which.Slug.Should().Be("ops");
        tenantDashboards.Should().BeEmpty();
        byId.Should().NotBeNull();
        bySlug.Should().NotBeNull();

        await service.Invoking(current => current.CalculateKpiAsync("missing", start, end, tenantId))
            .Should().ThrowAsync<ArgumentException>();
        var result = await service.CalculateKpiAsync("signups", start, end, tenantId);
        var hourly = await service.GetTimeSeriesAsync("signup", start, end, TimeSeriesGranularity.Hour, tenantId);
        var daily = await service.GetTimeSeriesAsync("signup", start, end, TimeSeriesGranularity.Day, tenantId);
        var weekly = await service.GetTimeSeriesAsync("signup", start, end, TimeSeriesGranularity.Week, tenantId);
        var monthly = await service.GetTimeSeriesAsync("signup", start, end, TimeSeriesGranularity.Month, tenantId);
        var fallback = await service.GetTimeSeriesAsync("signup", start, end, (TimeSeriesGranularity)999, tenantId);
        var aggregate = await service.AggregateEventsAsync("signup", ["event"], AggregationFunction.Count, start, end, tenantId);
        var funnel = await service.AnalyzeFunnelAsync(["login", "signup", "missing"], start, end, tenantId);
        await service.TrackEventsAsync([new AnalyticsEvent { EventName = "bulk", TenantId = tenantId }]);

        result.Value.Should().Be(1);
        hourly.Should().ContainSingle();
        daily.Should().ContainSingle();
        weekly.Should().ContainSingle();
        monthly.Should().ContainSingle();
        fallback.Should().ContainSingle();
        aggregate.Should().ContainSingle().Which.Value.Should().Be(1);
        funnel.Steps.Should().HaveCount(3);
    }

    private static AnalyticsTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AnalyticsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AnalyticsTestDbContext(options);
    }

    private sealed class AnalyticsTestDbContext(DbContextOptions<AnalyticsTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnalyticsEvent>();
            modelBuilder.Entity<KpiDefinition>();
            modelBuilder.Entity<Dashboard>();
            modelBuilder.Entity<DashboardWidget>();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
