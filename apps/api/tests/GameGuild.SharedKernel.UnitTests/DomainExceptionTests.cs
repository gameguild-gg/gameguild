using System.Net;
using FluentAssertions;


namespace GameGuild.Tests.SharedKernel.Unit;

public class DomainExceptionTests
{
    [Fact]
    public void InvalidStateTransition_ShouldSetProperties()
    {
        var ex = new InvalidStateTransitionException("Order", "Pending", "Completed");

        ex.EntityType.Should().Be("Order");
        ex.FromState.Should().Be("Pending");
        ex.ToState.Should().Be("Completed");
        ex.Message.Should().Contain("Order").And.Contain("Pending").And.Contain("Completed");
    }

    [Fact]
    public void EntityNotFound_ShouldSetProperties()
    {
        var id = Guid.NewGuid();
        var ex = new EntityNotFoundException("User", id);

        ex.EntityType.Should().Be("User");
        ex.EntityId.Should().Be(id);
        ex.Message.Should().Contain("User").And.Contain(id.ToString());
    }

    [Fact]
    public void SubscriptionNotFound_ShouldSetEntityType()
    {
        var id = Guid.NewGuid();
        var ex = new SubscriptionNotFoundException(id);

        ex.EntityType.Should().Be("Subscription");
        ex.EntityId.Should().Be(id);
    }
}

public class SecurityExceptionTests
{
    [Fact]
    public void AuthenticationRequired_ShouldReturn401()
    {
        var ex = new AuthenticationRequiredException();

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.PublicMessage.Should().Contain("Authentication");
        ex.InternalMessage.Should().Be("Authentication required");
    }

    [Fact]
    public void AuthenticationRequired_WithCustomMessage_ShouldUseIt()
    {
        var ex = new AuthenticationRequiredException("Token expired for user X");

        ex.InternalMessage.Should().Be("Token expired for user X");
        ex.PublicMessage.Should().Contain("Authentication");
    }

    [Fact]
    public void AuthenticationRequired_WithInnerException_ShouldWrap()
    {
        var inner = new InvalidOperationException("bad");
        var ex = new AuthenticationRequiredException("custom", inner);

        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void AccessDenied_ShouldReturn403()
    {
        var ex = new AccessDeniedException();

        ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.PublicMessage.Should().Contain("permission");
    }

    [Fact]
    public void AccessDenied_WithInnerException_ShouldWrap()
    {
        var inner = new Exception("inner");
        var ex = new AccessDeniedException("msg", inner);

        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void ForMissingPermission_ShouldIncludeDetails()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var ex = AccessDeniedException.ForMissingPermission(userId, "admin:write", tenantId, resourceId);

        ex.InternalMessage.Should().Contain(userId.ToString())
            .And.Contain("admin:write")
            .And.Contain(tenantId.ToString())
            .And.Contain(resourceId.ToString());
    }

    [Fact]
    public void ForMissingPermission_WithoutOptionals_ShouldOmitThem()
    {
        var userId = Guid.NewGuid();
        var ex = AccessDeniedException.ForMissingPermission(userId, "read");

        ex.InternalMessage.Should().Contain(userId.ToString()).And.Contain("read");
        ex.InternalMessage.Should().NotContain("tenant").And.NotContain("resource");
    }

    [Fact]
    public void ForTenantMembership_ShouldIncludeIds()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var ex = AccessDeniedException.ForTenantMembership(userId, tenantId);

        ex.InternalMessage.Should().Contain(userId.ToString())
            .And.Contain(tenantId.ToString());
    }

    [Fact]
    public void ForResourceOwnership_ShouldIncludeType()
    {
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var ex = AccessDeniedException.ForResourceOwnership(userId, "Document", resourceId);

        ex.InternalMessage.Should().Contain("Document")
            .And.Contain(resourceId.ToString());
    }

    [Fact]
    public void ForInactiveAccount_ShouldIncludeUserId()
    {
        var userId = Guid.NewGuid();
        var ex = AccessDeniedException.ForInactiveAccount(userId);

        ex.InternalMessage.Should().Contain(userId.ToString())
            .And.Contain("inactive");
    }

    [Fact]
    public void CrossTenantAccess_ShouldReturn403()
    {
        var userId = Guid.NewGuid();
        var attemptedTenantId = Guid.NewGuid();
        var userTenantId = Guid.NewGuid();

        var ex = new CrossTenantAccessException(userId, attemptedTenantId, userTenantId);

        ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.AttemptedTenantId.Should().Be(attemptedTenantId);
        ex.UserTenantId.Should().Be(userTenantId);
        ex.InternalMessage.Should().Contain(attemptedTenantId.ToString())
            .And.Contain(userTenantId.ToString());
    }

    [Fact]
    public void CrossTenantAccess_WithoutUserTenant_ShouldOmitIt()
    {
        var userId = Guid.NewGuid();
        var attemptedTenantId = Guid.NewGuid();

        var ex = new CrossTenantAccessException(userId, attemptedTenantId);

        ex.UserTenantId.Should().BeNull();
        ex.InternalMessage.Should().NotContain("user's tenant");
    }
}