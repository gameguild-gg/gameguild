using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using GameGuild.CQRS;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public sealed class ConditionalPolicyEvaluationControllerTests
{
    [Fact]
    public async Task EvaluateConditionalPolicies_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new EvaluateConditionalPoliciesCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConditionalPolicyResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.EvaluateConditionalPolicies(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task BulkEvaluateConditionalPolicies_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new BulkEvaluateConditionalPoliciesCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkConditionalPolicyResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.BulkEvaluateConditionalPolicies(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task TestConditionalPolicyRule_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new TestConditionalPolicyRuleCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConditionalPolicyTestResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.TestConditionalPolicyRule(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetConditionalPolicyStatistics_ShouldMapDefaultsWhenDatesMissing()
    {
        var mediator = new Mock<IMediator>();
        GetConditionalPolicyStatisticsQuery? captured = null;
        var tenantId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetConditionalPolicyStatisticsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetConditionalPolicyStatisticsQuery)request)
            .ReturnsAsync((ConditionalPolicyStatisticsDto)null!);

        var controller = CreateController(mediator);
        var before = DateTime.UtcNow;

        var result = await controller.GetConditionalPolicyStatistics(tenantId);

        var after = DateTime.UtcNow;
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.FromDate.Should().BeOnOrAfter(before.AddDays(-30).AddSeconds(-1));
        captured.FromDate.Should().BeOnOrBefore(after.AddDays(-30).AddSeconds(1));
        captured.ToDate.Should().BeOnOrAfter(before.AddSeconds(-1));
        captured.ToDate.Should().BeOnOrBefore(after.AddSeconds(1));
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetConditionalPolicyUsage_ShouldMapRouteAndDefaultDates()
    {
        var mediator = new Mock<IMediator>();
        GetConditionalPolicyUsageQuery? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetConditionalPolicyUsageQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetConditionalPolicyUsageQuery)request)
            .ReturnsAsync((ConditionalPolicyUsageDto)null!);

        var controller = CreateController(mediator);
        var before = DateTime.UtcNow;

        var result = await controller.GetConditionalPolicyUsage(policyId);

        var after = DateTime.UtcNow;
        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policyId);
        captured.FromDate.Should().BeOnOrAfter(before.AddDays(-7).AddSeconds(-1));
        captured.FromDate.Should().BeOnOrBefore(after.AddDays(-7).AddSeconds(1));
        captured.ToDate.Should().BeOnOrAfter(before.AddSeconds(-1));
        captured.ToDate.Should().BeOnOrBefore(after.AddSeconds(1));
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetConditionalPolicyEvaluationHistory_ShouldMapRouteAndPaging()
    {
        var mediator = new Mock<IMediator>();
        GetConditionalPolicyEvaluationHistoryQuery? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetConditionalPolicyEvaluationHistoryQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetConditionalPolicyEvaluationHistoryQuery)request)
            .ReturnsAsync((ConditionalPolicyEvaluationHistoryDto)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetConditionalPolicyEvaluationHistory(policyId, page: 2, pageSize: 15);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policyId);
        captured.Page.Should().Be(2);
        captured.PageSize.Should().Be(15);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task ValidateConditionalPolicy_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new ValidateConditionalPolicyCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConditionalPolicyValidationResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.ValidateConditionalPolicy(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetConditionalPolicyConflicts_ShouldMapTenantId()
    {
        var mediator = new Mock<IMediator>();
        GetConditionalPolicyConflictsQuery? captured = null;
        var tenantId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetConditionalPolicyConflictsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetConditionalPolicyConflictsQuery)request)
            .ReturnsAsync((ConditionalPolicyConflictsDto)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetConditionalPolicyConflicts(tenantId);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task SimulateConditionalPolicy_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new SimulateConditionalPolicyCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConditionalPolicySimulationResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.SimulateConditionalPolicy(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetPolicyConditionTypes_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var conditionTypes = Array.Empty<PolicyConditionTypeDto>();

        mediator
            .Setup(x => x.Send(It.IsAny<GetPolicyConditionTypesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conditionTypes.AsEnumerable());

        var controller = CreateController(mediator);

        var result = await controller.GetPolicyConditionTypes();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(conditionTypes);
    }

    [Fact]
    public async Task ValidateCondition_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new ValidateConditionCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConditionValidationResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.ValidateCondition(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    private static ConditionalPolicyEvaluationController CreateController(Mock<IMediator> mediator)
    {
        return new ConditionalPolicyEvaluationController(mediator.Object, NullLogger<ConditionalPolicyEvaluationController>.Instance);
    }
}