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

    // NOTE (issue #1203): the analytics read tests (statistics, usage, evaluation
    // history, conflicts, condition types) previously mocked IMediator and stayed green
    // while the endpoints had no handlers and answered 500. They are replaced by
    // pipeline-level coverage in GameGuild.API.IntegrationTests
    // (PolicyManagementQueriesPostgreSqlHttpTests).

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