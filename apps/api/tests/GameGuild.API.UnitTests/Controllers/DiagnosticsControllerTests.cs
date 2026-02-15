using FluentAssertions;
using GameGuild.API.Controllers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.API.UnitTests.Controllers;

public class DiagnosticsControllerTests
{
    private readonly Mock<ILogger<DiagnosticsController>> _loggerMock = new();

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var act = () => new DiagnosticsController(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetInfo_ShouldReturnOkResult()
    {
        var controller = new DiagnosticsController(_loggerMock.Object);

        var result = controller.GetInfo();

        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>();
    }

    [Fact]
    public void GetInfo_ShouldReturnApplicationDetails()
    {
        var controller = new DiagnosticsController(_loggerMock.Object);

        var ok = result(controller);
        var response = ok.Value as ApplicationInfoResponse;

        response.Should().NotBeNull();
        response!.Application.Should().NotBeNull();
        response.Application.Name.Should().NotBeNullOrEmpty();
        response.Application.Version.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetInfo_ShouldReturnBuildDetails()
    {
        var controller = new DiagnosticsController(_loggerMock.Object);

        var ok = result(controller);
        var response = ok.Value as ApplicationInfoResponse;

        response!.Build.Should().NotBeNull();
        response.Build.Framework.Should().NotBeNullOrEmpty();
        response.Build.Configuration.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetInfo_ShouldReturnRuntimeDetails()
    {
        var controller = new DiagnosticsController(_loggerMock.Object);

        var ok = result(controller);
        var response = ok.Value as ApplicationInfoResponse;

        response!.Runtime.Should().NotBeNull();
        response.Runtime.DotNetVersion.Should().NotBeNullOrEmpty();
        response.Runtime.OSDescription.Should().NotBeNullOrEmpty();
        response.Runtime.OSArchitecture.Should().NotBeNullOrEmpty();
        response.Runtime.ProcessArchitecture.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetInfo_ShouldReturnProcessDetails()
    {
        var controller = new DiagnosticsController(_loggerMock.Object);

        var ok = result(controller);
        var response = ok.Value as ApplicationInfoResponse;

        response!.Process.Should().NotBeNull();
        response.Process.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetInfo_ShouldSetTimestamp()
    {
        var controller = new DiagnosticsController(_loggerMock.Object);

        var ok = result(controller);
        var response = ok.Value as ApplicationInfoResponse;

        response!.Timestamp.Should().BeAfter(DateTime.MinValue);
    }

    // Helper to extract OkObjectResult
    private static Microsoft.AspNetCore.Mvc.OkObjectResult result(DiagnosticsController controller)
    {
        var actionResult = controller.GetInfo();
        return actionResult.Result as Microsoft.AspNetCore.Mvc.OkObjectResult
               ?? throw new InvalidOperationException("Expected OkObjectResult");
    }
}

public class ApplicationInfoResponseModelTests
{
    [Fact]
    public void ApplicationDetails_ShouldHaveDefaults()
    {
        var details = new ApplicationDetails();

        details.Name.Should().BeEmpty();
        details.Version.Should().BeEmpty();
        details.InformationalVersion.Should().BeEmpty();
        details.Description.Should().BeEmpty();
    }

    [Fact]
    public void BuildDetails_ShouldHaveDefaults()
    {
        var details = new BuildDetails();

        details.Timestamp.Should().BeNull();
        details.Configuration.Should().BeEmpty();
        details.Framework.Should().BeEmpty();
    }

    [Fact]
    public void RuntimeDetails_ShouldHaveDefaults()
    {
        var details = new RuntimeDetails();

        details.DotNetVersion.Should().BeEmpty();
        details.OSDescription.Should().BeEmpty();
        details.OSArchitecture.Should().BeEmpty();
        details.ProcessArchitecture.Should().BeEmpty();
    }

    [Fact]
    public void ProcessDetails_ShouldHaveDefaults()
    {
        var details = new ProcessDetails();

        details.StartTime.Should().Be(default);
        details.Uptime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ApplicationInfoResponse_ShouldHaveDefaults()
    {
        var response = new ApplicationInfoResponse();

        response.Application.Should().NotBeNull();
        response.Build.Should().NotBeNull();
        response.Runtime.Should().NotBeNull();
        response.Process.Should().NotBeNull();
    }
}
