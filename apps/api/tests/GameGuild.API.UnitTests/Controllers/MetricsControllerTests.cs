using FluentAssertions;
using GameGuild.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.API.UnitTests.Controllers;

public class MetricsControllerTests
{
    private readonly Mock<ILogger<MetricsController>> _loggerMock = new();

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var act = () => new MetricsController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetMetrics_ShouldReturnContentResult()
    {
        var controller = new MetricsController(_loggerMock.Object);

        var result = controller.GetMetrics();

        result.Should().BeOfType<ContentResult>();
    }

    [Fact]
    public void GetMetrics_ShouldReturnPrometheusFormat()
    {
        var controller = new MetricsController(_loggerMock.Object);

        var result = controller.GetMetrics() as ContentResult;

        result!.ContentType.Should().Be("text/plain; version=0.0.4; charset=utf-8");
        result.Content.Should().Contain("process_cpu_seconds_total");
        result.Content.Should().Contain("process_virtual_memory_bytes");
        result.Content.Should().Contain("process_working_set_bytes");
        result.Content.Should().Contain("dotnet_gc_collections_total");
        result.Content.Should().Contain("app_info");
    }

    [Fact]
    public void GetMetrics_ShouldContainHelpAndTypeAnnotations()
    {
        var controller = new MetricsController(_loggerMock.Object);

        var result = controller.GetMetrics() as ContentResult;

        result!.Content.Should().Contain("# HELP");
        result.Content.Should().Contain("# TYPE");
        result.Content.Should().NotContain("\r");
    }

    [Fact]
    public void GetMetrics_ShouldContainGcGenerations()
    {
        var controller = new MetricsController(_loggerMock.Object);

        var result = controller.GetMetrics() as ContentResult;

        result!.Content.Should().Contain("generation=\"0\"");
        result!.Content.Should().Contain("generation=\"1\"");
        result!.Content.Should().Contain("generation=\"2\"");
    }

    [Fact]
    public void GetMetrics_ShouldContainUptimeMetric()
    {
        var controller = new MetricsController(_loggerMock.Object);

        var result = controller.GetMetrics() as ContentResult;

        result!.Content.Should().Contain("process_uptime_seconds");
        result.Content.Should().Contain("process_start_time_seconds");
    }

    [Fact]
    public void GetMetrics_ShouldContainThreadCount()
    {
        var controller = new MetricsController(_loggerMock.Object);

        var result = controller.GetMetrics() as ContentResult;

        result!.Content.Should().Contain("process_num_threads");
    }
}
