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
    public async Task GetAbacPolicy_ShouldMapPolicyIdAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        GetAbacPolicyQuery? captured = null;
        var policy = CreatePolicy();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAbacPolicyQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAbacPolicyQuery)request)
            .ReturnsAsync(policy);

        var controller = CreateController(mediator);

        var result = await controller.GetAbacPolicy(policy.Id);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policy.Id);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(policy);
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
    public async Task GetAbacPolicies_ShouldMapFiltersAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        GetAbacPoliciesQuery? captured = null;
        var tenantId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAbacPoliciesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAbacPoliciesQuery)request)
            .ReturnsAsync((PagedResult<AbacPolicy>)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetAbacPolicies(tenantId, isActive: true, category: "Security", page: 2, pageSize: 15);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.IsActive.Should().BeTrue();
        captured.Category.Should().Be("Security");
        captured.Page.Should().Be(2);
        captured.PageSize.Should().Be(15);
        result.Result.Should().BeOfType<OkObjectResult>();
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
    public async Task GetAbacPolicyStatistics_ShouldMapDefaultDates()
    {
        var mediator = new Mock<IMediator>();
        GetAbacPolicyStatisticsQuery? captured = null;
        var tenantId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAbacPolicyStatisticsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAbacPolicyStatisticsQuery)request)
            .ReturnsAsync((AbacPolicyStatisticsDto)null!);

        var controller = CreateController(mediator);
        var before = DateTime.UtcNow;

        var result = await controller.GetAbacPolicyStatistics(tenantId);

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
    public async Task GetAbacPolicyUsage_ShouldMapPolicyIdAndDefaultDates()
    {
        var mediator = new Mock<IMediator>();
        GetAbacPolicyUsageQuery? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAbacPolicyUsageQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAbacPolicyUsageQuery)request)
            .ReturnsAsync((AbacPolicyUsageDto)null!);

        var controller = CreateController(mediator);
        var before = DateTime.UtcNow;

        var result = await controller.GetAbacPolicyUsage(policyId);

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
    public async Task GetAbacPolicyAuditTrail_ShouldMapPolicyIdAndPaging()
    {
        var mediator = new Mock<IMediator>();
        GetAbacPolicyAuditTrailQuery? captured = null;
        var policyId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAbacPolicyAuditTrailQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAbacPolicyAuditTrailQuery)request)
            .ReturnsAsync((AbacPolicyAuditTrailDto)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetAbacPolicyAuditTrail(policyId, page: 4, pageSize: 12);

        captured.Should().NotBeNull();
        captured!.PolicyId.Should().Be(policyId);
        captured.Page.Should().Be(4);
        captured.PageSize.Should().Be(12);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
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
    public async Task GetAbacPolicyConflicts_ShouldMapTenantId()
    {
        var mediator = new Mock<IMediator>();
        GetAbacPolicyConflictsQuery? captured = null;
        var tenantId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAbacPolicyConflictsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAbacPolicyConflictsQuery)request)
            .ReturnsAsync((AbacPolicyConflictsDto)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetAbacPolicyConflicts(tenantId);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetAbacPolicyTemplates_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var templates = Array.Empty<AbacPolicyTemplateDto>();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAbacPolicyTemplatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates.AsEnumerable());

        var controller = CreateController(mediator);

        var result = await controller.GetAbacPolicyTemplates();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(templates);
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