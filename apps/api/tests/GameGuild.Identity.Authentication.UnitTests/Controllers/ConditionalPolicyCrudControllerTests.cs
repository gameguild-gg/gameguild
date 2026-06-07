using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public sealed class ConditionalPolicyCrudControllerTests
{
    [Fact]
    public async Task CreateConditionalPolicy_ShouldReturnCreatedAtAction()
    {
        var mediator = new Mock<IMediator>();
        var command = new CreateConditionalPolicyCommand();
        var policy = CreatePolicy();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var controller = CreateController(mediator);

        var result = await controller.CreateConditionalPolicy(command);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(ConditionalPolicyCrudController.GetConditionalPolicy));
        created.RouteValues!["id"].Should().Be(policy.Id);
        created.Value.Should().BeSameAs(policy);
    }

    [Fact]
    public async Task GetConditionalPolicy_ShouldReturnOkAndMapPolicyId()
    {
        var mediator = new Mock<IMediator>();
        GetConditionalPolicyQuery? captured = null;
        var policy = CreatePolicy();

        mediator
            .Setup(x => x.Send(It.IsAny<GetConditionalPolicyQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetConditionalPolicyQuery)request)
            .ReturnsAsync(policy);

        var controller = CreateController(mediator);

        var result = await controller.GetConditionalPolicy(policy.Id);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policy.Id);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(policy);
    }

    [Fact]
    public async Task UpdateConditionalPolicy_ShouldAssignPolicyIdAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var policy = CreatePolicy();
        var command = new UpdateConditionalPolicyCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var controller = CreateController(mediator);

        var result = await controller.UpdateConditionalPolicy(policy.Id, command);

        command.PolicyId.Should().Be(policy.Id);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(policy);
    }

    [Fact]
    public async Task DeleteConditionalPolicy_ShouldMapPolicyIdAndReturnNoContent()
    {
        var mediator = new Mock<IMediator>();
        DeleteConditionalPolicyCommand? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<DeleteConditionalPolicyCommand>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteConditionalPolicyCommand, CancellationToken>((request, _) => captured = request)
            .Returns(Task.CompletedTask);

        var controller = CreateController(mediator);

        var result = await controller.DeleteConditionalPolicy(policyId);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policyId);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetConditionalPolicies_ShouldMapFiltersAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        GetConditionalPoliciesQuery? captured = null;
        var tenantId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetConditionalPoliciesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetConditionalPoliciesQuery)request)
            .ReturnsAsync((PagedResult<ConditionalPolicy>)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetConditionalPolicies(tenantId, isActive: true, conditionType: "TimeBased", page: 3, pageSize: 25);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.IsActive.Should().BeTrue();
        captured.ConditionType.Should().Be("TimeBased");
        captured.Page.Should().Be(3);
        captured.PageSize.Should().Be(25);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ActivateConditionalPolicy_ShouldReturnSuccessMessage()
    {
        var mediator = new Mock<IMediator>();
        ActivateConditionalPolicyCommand? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<ActivateConditionalPolicyCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ActivateConditionalPolicyCommand, CancellationToken>((request, _) => captured = request)
            .Returns(Task.CompletedTask);

        var controller = CreateController(mediator);

        var result = await controller.ActivateConditionalPolicy(policyId);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policyId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        GetAnonymousProperty<string>(ok.Value!, "message").Should().Be("Conditional policy activated successfully");
    }

    [Fact]
    public async Task DeactivateConditionalPolicy_ShouldReturnSuccessMessage()
    {
        var mediator = new Mock<IMediator>();
        DeactivateConditionalPolicyCommand? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<DeactivateConditionalPolicyCommand>(), It.IsAny<CancellationToken>()))
            .Callback<DeactivateConditionalPolicyCommand, CancellationToken>((request, _) => captured = request)
            .Returns(Task.CompletedTask);

        var controller = CreateController(mediator);

        var result = await controller.DeactivateConditionalPolicy(policyId);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policyId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        GetAnonymousProperty<string>(ok.Value!, "message").Should().Be("Conditional policy deactivated successfully");
    }

    [Fact]
    public async Task CloneConditionalPolicy_ShouldAssignSourcePolicyIdAndReturnCreatedAtAction()
    {
        var mediator = new Mock<IMediator>();
        var command = new CloneConditionalPolicyCommand();
        var clonedPolicy = CreatePolicy();
        var sourcePolicyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clonedPolicy);

        var controller = CreateController(mediator);

        var result = await controller.CloneConditionalPolicy(sourcePolicyId, command);

        command.SourcePolicyId.Should().Be(sourcePolicyId);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.RouteValues!["policyId"].Should().Be(clonedPolicy.Id);
        created.Value.Should().BeSameAs(clonedPolicy);
    }

    [Fact]
    public async Task UpdateConditionalPolicyPriority_ShouldAssignPolicyIdAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new UpdateConditionalPolicyPriorityCommand();
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(mediator);

        var result = await controller.UpdateConditionalPolicyPriority(policyId, command);

        command.PolicyId.Should().Be(policyId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        GetAnonymousProperty<string>(ok.Value!, "message").Should().Be("Policy priority updated successfully");
    }

    [Fact]
    public async Task GetConditionalPolicyTemplates_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var templates = Array.Empty<ConditionalPolicyTemplateDto>();

        mediator
            .Setup(x => x.Send(It.IsAny<GetConditionalPolicyTemplatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates.AsEnumerable());

        var controller = CreateController(mediator);

        var result = await controller.GetConditionalPolicyTemplates();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(templates);
    }

    [Fact]
    public async Task CreateConditionalPolicyFromTemplate_ShouldAssignTemplateIdAndReturnCreatedAtAction()
    {
        var mediator = new Mock<IMediator>();
        var command = new CreateConditionalPolicyFromTemplateCommand();
        var policy = CreatePolicy();
        var templateId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var controller = CreateController(mediator);

        var result = await controller.CreateConditionalPolicyFromTemplate(templateId, command);

        command.TemplateId.Should().Be(templateId);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.RouteValues!["id"].Should().Be(policy.Id);
        created.Value.Should().BeSameAs(policy);
    }

    private static ConditionalPolicyCrudController CreateController(Mock<IMediator> mediator)
    {
        return new ConditionalPolicyCrudController(mediator.Object, NullLogger<ConditionalPolicyCrudController>.Instance);
    }

    private static ConditionalPolicy CreatePolicy()
    {
        return new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Conditional Policy",
            CreatedBy = Guid.NewGuid()
        };
    }

    private static T GetAnonymousProperty<T>(object target, string propertyName)
    {
        return (T)target.GetType().GetProperty(propertyName)!.GetValue(target)!;
    }
}