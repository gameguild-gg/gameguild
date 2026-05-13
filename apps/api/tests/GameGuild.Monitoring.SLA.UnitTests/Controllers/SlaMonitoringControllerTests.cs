using FluentAssertions;

using Microsoft.AspNetCore.Mvc;

using Moq;

using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

using Xunit;

namespace GameGuild.Monitoring.SLA.UnitTests.Controllers;

public class SlaMonitoringControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<IActorContextAccessor> _actorContextAccessor = new();
    private readonly SlaMonitoringController _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public SlaMonitoringControllerTests()
    {
        _actorContextAccessor.Setup(accessor => accessor.ActorContext)
            .Returns(new ActorContext
            {
                TenantId = _tenantId,
                ActorKind = ActorKind.User,
                Roles = new HashSet<string>(),
                Permissions = new HashSet<string>(),
                IsAuthenticated = true
            });

        _sut = new SlaMonitoringController(_sender.Object, _actorContextAccessor.Object);
    }

    [Fact]
    public async Task GetSlos_ShouldReturnOk()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<GetSlosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloDto>());

        var result = await _sut.GetSlos(cancellationToken: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSlo_Found_ShouldReturnOk()
    {
        var sloId = Guid.NewGuid();

        _sender.Setup(sender => sender.Send(It.IsAny<GetSloByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SloDto { Id = sloId, Name = "Test" });

        var result = await _sut.GetSlo(sloId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSlo_NotFound_ShouldReturnNotFound()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<GetSloByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SloDto?)null);

        var result = await _sut.GetSlo(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateSlo_ShouldReturnCreated()
    {
        var command = new CreateSloCommand(_tenantId, "Test SLO", null, "test-service", 99.9, 30, 0.1, 50);
        var resultDto = new SloDto { Id = Guid.NewGuid(), Name = "Test SLO" };

        _sender.Setup(sender => sender.Send(It.IsAny<CreateSloCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _sut.CreateSlo(command, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateSlo_IdMismatch_ShouldReturnBadRequest()
    {
        var commandId = Guid.NewGuid();
        var command = new UpdateSloCommand(commandId, _tenantId, "Name", null, "Svc", 99.9, 30, 0.1, 50, true);

        var result = await _sut.UpdateSlo(Guid.NewGuid(), command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateSlo_SameId_ShouldReturnOk()
    {
        var id = Guid.NewGuid();
        var command = new UpdateSloCommand(id, _tenantId, "Name", null, "Svc", 99.9, 30, 0.1, 50, true);
        var resultDto = new SloDto { Id = id, Name = "Name" };

        _sender.Setup(sender => sender.Send(It.IsAny<UpdateSloCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _sut.UpdateSlo(id, command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteSlo_ShouldReturnNoContent()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<DeleteSloCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _sut.DeleteSlo(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RecordSliMetric_ShouldReturnNoContent()
    {
        var command = new RecordSliMetricCommand(_tenantId, Guid.NewGuid(), true, 99.9);

        _sender.Setup(sender => sender.Send(It.IsAny<RecordSliMetricCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SliMetricDto());

        var result = await _sut.RecordSliMetric(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetCompliance_ShouldReturnOk()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<GetSloComplianceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SloComplianceDto());

        var result = await _sut.GetCompliance(Guid.NewGuid(), cancellationToken: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetErrorBudget_ShouldReturnOk()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<GetErrorBudgetQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorBudgetDto());

        var result = await _sut.GetErrorBudget(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetViolations_ShouldReturnOk()
    {
        _sender.Setup(sender => sender.Send(It.IsAny<GetSloViolationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SloViolationDto>());

        var result = await _sut.GetViolations(cancellationToken: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResolveViolation_IdMismatch_ShouldReturnBadRequest()
    {
        var command = new TestResolveSloViolationCommand(Guid.NewGuid(), _tenantId, "note");

        var result = await _sut.ResolveViolation(Guid.NewGuid(), command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResolveViolation_ShouldReturnNoContent()
    {
        var id = Guid.NewGuid();
        var command = new TestResolveSloViolationCommand(id, _tenantId, "resolved");

        _sender.Setup(sender => sender.Send(It.IsAny<ResolveSloViolationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _sut.ResolveViolation(id, command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    private sealed record TestResolveSloViolationCommand(Guid ViolationId, Guid TenantId, string? ResolutionNotes = null)
        : ResolveSloViolationCommand(ViolationId, TenantId, ResolutionNotes);
}