using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class TenantValidationExtensionsTests
{
    [Fact]
    public void ValidateTenantAccess_Should_Allow_Anonymous()
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.Setup(a => a.ActorContext).Returns(ActorContext.Anonymous);

        var result = accessor.Object.ValidateTenantAccess(Guid.NewGuid(), "create tenant");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTenantAccess_Should_Reject_When_No_Tenant()
    {
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).Build();
        var accessor = new Mock<IActorContextAccessor>();
        accessor.Setup(a => a.ActorContext).Returns(context);

        var result = accessor.Object.ValidateTenantAccess(Guid.NewGuid(), "update settings");

        result.IsValid.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public void ValidateTenantAccess_Should_Reject_When_Tenant_Mismatch()
    {
        var tenantId = Guid.NewGuid();
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(tenantId).Build();
        var accessor = new Mock<IActorContextAccessor>();
        accessor.Setup(a => a.ActorContext).Returns(context);

        var result = accessor.Object.ValidateTenantAccess(Guid.NewGuid(), "delete resource");

        result.IsValid.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.ErrorDetails.Should().NotBeNull();
    }

    [Fact]
    public void ValidateTenantAccess_Should_Succeed_When_Tenant_Matches()
    {
        var tenantId = Guid.NewGuid();
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(tenantId).Build();
        var accessor = new Mock<IActorContextAccessor>();
        accessor.Setup(a => a.ActorContext).Returns(context);

        var result = accessor.Object.ValidateTenantAccess(tenantId, "read resource");

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.StatusCode.Should().BeNull();
        result.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void ValidateTenantAccessAsActionResult_Should_Return_ObjectResult_When_Failed()
    {
        var tenantId = Guid.NewGuid();
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(tenantId).Build();
        var accessor = new Mock<IActorContextAccessor>();
        accessor.Setup(a => a.ActorContext).Returns(context);

        var actionResult = accessor.Object.ValidateTenantAccessAsActionResult(Guid.NewGuid(), "update resource");

        actionResult.Should().BeOfType<ObjectResult>();
        actionResult!.As<ObjectResult>().StatusCode.Should().Be(403);
    }

    [Fact]
    public void ValidateTenantAccessAsActionResult_Should_Return_Null_When_Valid()
    {
        var tenantId = Guid.NewGuid();
        var context = ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(tenantId).Build();
        var accessor = new Mock<IActorContextAccessor>();
        accessor.Setup(a => a.ActorContext).Returns(context);

        var actionResult = accessor.Object.ValidateTenantAccessAsActionResult(tenantId, "read resource");

        actionResult.Should().BeNull();
    }

    [Fact]
    public void TenantValidationResult_Factories_Should_Produce_Expected_ActionResults()
    {
        var success = TenantValidationResult.Success();
        var forbidden = TenantValidationResult.Forbidden("User is not associated with any tenant");
        var crossTenant = TenantValidationResult.CrossTenantDenied(Guid.NewGuid(), Guid.NewGuid(), "delete resource");

        success.ToActionResult().Should().BeNull();

        var forbiddenResult = forbidden.ToActionResult().Should().BeOfType<ObjectResult>().Subject;
        forbiddenResult.StatusCode.Should().Be(403);
        forbiddenResult.Value.Should().BeEquivalentTo(new { error = "User is not associated with any tenant" });

        var crossTenantResult = crossTenant.ToActionResult().Should().BeOfType<ObjectResult>().Subject;
        crossTenantResult.StatusCode.Should().Be(403);
        crossTenantResult.Value.Should().BeEquivalentTo(crossTenant.ErrorDetails);
    }
}
