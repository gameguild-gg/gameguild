using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public sealed class AbacPolicyControllerTests
{
    // NOTE (issue #1203): the read-side tests (GetAbacPolicy, GetAbacPolicies, statistics,
    // usage, audit-trail, conflicts, templates) previously mocked IMediator and stayed
    // green while the endpoints had no handlers and answered 500. They are replaced by
    // pipeline-level coverage in GameGuild.API.IntegrationTests
    // (PolicyManagementQueriesPostgreSqlHttpTests), which exercises the real mediator
    // and handlers. The remaining tests below cover the still-unavailable command
    // surface (422 mapping), which is real controller behavior.

    [Fact]
    public async Task CreateAbacPolicy_ShouldReturnCreatedAtAction()
    {
        var mediator = new Mock<IMediator>();
        var command = new CreateAbacPolicyCommand();
        var policy = CreatePolicy();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var controller = CreateController(mediator);

        var result = await controller.CreateAbacPolicy(command);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(AbacPolicyController.GetAbacPolicy));
        created.RouteValues!["id"].Should().Be(policy.Id);
        created.Value.Should().BeSameAs(policy);
    }

    [Fact]
    public async Task UpdateAbacPolicy_ShouldAssignPolicyIdAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        UpdateAbacPolicyCommand? captured = null;
        var policy = CreatePolicy();
        var command = new UpdateAbacPolicyCommand();

        mediator
            .Setup(x => x.Send(It.IsAny<UpdateAbacPolicyCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (UpdateAbacPolicyCommand)request)
            .ReturnsAsync(policy);

        var controller = CreateController(mediator);

        var result = await controller.UpdateAbacPolicy(policy.Id, command);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policy.Id);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(policy);
    }

    [Fact]
    public async Task DeleteAbacPolicy_ShouldReturnNoContent()
    {
        var mediator = new Mock<IMediator>();
        DeleteAbacPolicyCommand? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<DeleteAbacPolicyCommand>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteAbacPolicyCommand, CancellationToken>((request, _) => captured = request)
            .Returns(Task.CompletedTask);

        var controller = CreateController(mediator);

        var result = await controller.DeleteAbacPolicy(policyId);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policyId);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task EvaluateAbacPolicies_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new EvaluateAbacPoliciesCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AbacEvaluationResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.EvaluateAbacPolicies(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task BulkEvaluateAbacPolicies_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new BulkEvaluateAbacPoliciesCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkAbacEvaluationResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.BulkEvaluateAbacPolicies(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task TestAbacExpression_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new TestAbacExpressionCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AbacExpressionTestResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.TestAbacExpression(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task ActivateAbacPolicy_ShouldReturnSuccessMessage()
    {
        var mediator = new Mock<IMediator>();
        ActivateAbacPolicyCommand? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<ActivateAbacPolicyCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ActivateAbacPolicyCommand, CancellationToken>((request, _) => captured = request)
            .Returns(Task.CompletedTask);

        var controller = CreateController(mediator);

        var result = await controller.ActivateAbacPolicy(policyId);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policyId);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        GetAnonymousProperty<string>(ok.Value!, "message").Should().Be("ABAC policy activated successfully");
    }

    [Fact]
    public async Task DeactivateAbacPolicy_ShouldReturnSuccessMessage()
    {
        var mediator = new Mock<IMediator>();
        DeactivateAbacPolicyCommand? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<DeactivateAbacPolicyCommand>(), It.IsAny<CancellationToken>()))
            .Callback<DeactivateAbacPolicyCommand, CancellationToken>((request, _) => captured = request)
            .Returns(Task.CompletedTask);

        var controller = CreateController(mediator);

        var result = await controller.DeactivateAbacPolicy(policyId);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policyId);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        GetAnonymousProperty<string>(ok.Value!, "message").Should().Be("ABAC policy deactivated successfully");
    }

    [Fact]
    public async Task CloneAbacPolicy_ShouldAssignSourcePolicyIdAndReturnCreatedAtAction()
    {
        var mediator = new Mock<IMediator>();
        CloneAbacPolicyCommand? captured = null;
        var policyId = Guid.NewGuid();
        var command = new CloneAbacPolicyCommand();
        var clonedPolicy = CreatePolicy();

        mediator
            .Setup(x => x.Send(It.IsAny<CloneAbacPolicyCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (CloneAbacPolicyCommand)request)
            .ReturnsAsync(clonedPolicy);

        var controller = CreateController(mediator);

        var result = await controller.CloneAbacPolicy(policyId, command);

        captured.Should().NotBeNull();
        captured!.SourcePolicyId.Should().Be(policyId);
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(AbacPolicyController.GetAbacPolicy));
        created.RouteValues!["policyId"].Should().Be(clonedPolicy.Id);
        created.Value.Should().BeSameAs(clonedPolicy);
    }

    [Fact]
    public async Task ValidateAbacPolicy_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new ValidateAbacPolicyCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AbacPolicyValidationResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.ValidateAbacPolicy(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task CreateAbacPolicyFromTemplate_ShouldAssignTemplateIdAndReturnCreatedAtAction()
    {
        var mediator = new Mock<IMediator>();
        CreateAbacPolicyFromTemplateCommand? captured = null;
        var templateId = Guid.NewGuid();
        var command = new CreateAbacPolicyFromTemplateCommand();
        var policy = CreatePolicy();

        mediator
            .Setup(x => x.Send(It.IsAny<CreateAbacPolicyFromTemplateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (CreateAbacPolicyFromTemplateCommand)request)
            .ReturnsAsync(policy);

        var controller = CreateController(mediator);

        var result = await controller.CreateAbacPolicyFromTemplate(templateId, command);

        captured.Should().NotBeNull();
        captured!.TemplateId.Should().Be(templateId);
        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(AbacPolicyController.GetAbacPolicy));
        created.RouteValues!["id"].Should().Be(policy.Id);
        created.Value.Should().BeSameAs(policy);
    }

    private static AbacPolicyController CreateController(Mock<IMediator> mediator)
    {
        return new AbacPolicyController(mediator.Object, NullLogger<AbacPolicyController>.Instance);
    }

    private static AbacPolicy CreatePolicy()
    {
        return new AbacPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Policy",
            Priority = 1,
            AttributeExpression = "{}"
        };
    }

    private static T GetAnonymousProperty<T>(object target, string propertyName)
    {
        return (T)target.GetType().GetProperty(propertyName)!.GetValue(target)!;
    }
}