using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class ActorContextBuilderTests
{
    [Fact]
    public void ForUser_Should_Map_UserActor_Metadata()
    {
        var actor = new UserActor(Guid.NewGuid(), "user@example.com", "User Name");

        var context = ActorContextBuilder.ForUser(actor).Build();

        context.ActorKind.Should().Be(ActorKind.User);
        context.SubjectId.Should().Be(actor.UserId.ToString());
        context.GetAttribute("email").Should().Be("user@example.com");
        context.GetAttribute("name").Should().Be("User Name");
    }

    [Fact]
    public void ForUser_Should_Set_User_Context()
    {
        var userId = Guid.NewGuid();

        var context = ActorContextBuilder.ForUser(userId).Build();

        context.ActorKind.Should().Be(ActorKind.User);
        context.SubjectId.Should().Be(userId.ToString());
        context.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void ForService_Should_Set_Service_Attributes()
    {
        var context = ActorContextBuilder.ForService("service-id", "BillingService").Build();

        context.ActorKind.Should().Be(ActorKind.Service);
        context.TypedAttributes.GetCustomAttribute("service_name").Should().Be("BillingService");
    }

    [Fact]
    public void ForSystem_Should_Assign_SystemAdmin_Role()
    {
        var context = ActorContextBuilder.ForSystem("DailyJob").Build();

        context.ActorKind.Should().Be(ActorKind.System);
        context.Roles.Should().Contain("SystemAdmin");
        context.SubjectId.Should().Be(SystemActor.SystemSubjectId);
    }

    [Fact]
    public void WithMfaVerified_Should_Set_Typed_Attribute()
    {
        var context = ActorContextBuilder.Create()
            .WithMfaVerified()
            .AsAuthenticated()
            .Build();

        context.TypedAttributes.MfaVerified.Should().BeTrue();
    }

    [Fact]
    public void WithTypedAttributes_Should_Merge_Attributes()
    {
        var typed = new ActorAttributes { Email = "user@example.com", TenantRole = "Owner" };

        var context = ActorContextBuilder.Create()
            .WithTypedAttributes(typed)
            .Build();

        context.TypedAttributes.Email.Should().Be("user@example.com");
        context.TypedAttributes.TenantRole.Should().Be("Owner");
    }

    [Fact]
    public void Builder_Should_Apply_All_Fluent_Configuration()
    {
        var tenantId = Guid.NewGuid();

        var context = ActorContextBuilder.Create()
            .WithActorKind(ActorKind.Service)
            .WithSubjectId("svc-1")
            .WithTenantId(tenantId)
            .WithRole("Admin")
            .WithRoles(["admin", "Member"])
            .WithPermission("users:read")
            .WithPermissions(["USERS:READ", "users:write"])
            .WithAttribute("region", "BR")
            .WithAttributes([new KeyValuePair<string, string>("department", "Finance")])
            .WithAuthScheme("ApiKey")
            .WithMfaVerified(false)
            .AsAuthenticated()
            .Build();

        context.ActorKind.Should().Be(ActorKind.Service);
        context.SubjectId.Should().Be("svc-1");
        context.TenantId.Should().Be(tenantId);
        context.Roles.Should().BeEquivalentTo(["Admin", "Member"]);
        context.Permissions.Should().BeEquivalentTo(["users:read", "users:write"]);
        context.AuthScheme.Should().Be("ApiKey");
        context.GetAttribute("region").Should().Be("BR");
        context.GetAttribute("department").Should().Be("Finance");
        context.IsAuthenticated.Should().BeTrue();
        context.IsMfaVerified.Should().BeFalse();
    }

    [Fact]
    public void Factory_Methods_Should_Validate_Null_Arguments()
    {
        var nullUserActor = () => ActorContextBuilder.ForUser((UserActor)null!);
        var nullServiceId = () => ActorContextBuilder.ForService(null!, "service");
        var nullServiceName = () => ActorContextBuilder.ForService("service", null!);
        var nullOperation = () => ActorContextBuilder.ForSystem(null!);

        nullUserActor.Should().Throw<ArgumentNullException>().WithParameterName("actor");
        nullServiceId.Should().Throw<ArgumentNullException>().WithParameterName("serviceId");
        nullServiceName.Should().Throw<ArgumentNullException>().WithParameterName("serviceName");
        nullOperation.Should().Throw<ArgumentNullException>().WithParameterName("operationName");
    }

    [Fact]
    public void Fluent_Methods_Should_Validate_Null_Arguments()
    {
        var builder = ActorContextBuilder.Create();

        var nullRole = () => builder.WithRole(null!);
        var nullRoles = () => builder.WithRoles(null!);
        var nullPermission = () => builder.WithPermission(null!);
        var nullPermissions = () => builder.WithPermissions(null!);
        var nullAttributeKey = () => builder.WithAttribute(null!, "value");
        var nullAttributeValue = () => builder.WithAttribute("key", null!);
        var nullAttributes = () => builder.WithAttributes(null!);
        var nullTypedAttributes = () => builder.WithTypedAttributes(null!);

        nullRole.Should().Throw<ArgumentNullException>().WithParameterName("role");
        nullRoles.Should().Throw<ArgumentNullException>().WithParameterName("roles");
        nullPermission.Should().Throw<ArgumentNullException>().WithParameterName("permission");
        nullPermissions.Should().Throw<ArgumentNullException>().WithParameterName("permissions");
        nullAttributeKey.Should().Throw<ArgumentNullException>().WithParameterName("key");
        nullAttributeValue.Should().Throw<ArgumentNullException>().WithParameterName("value");
        nullAttributes.Should().Throw<ArgumentNullException>().WithParameterName("attributes");
        nullTypedAttributes.Should().Throw<ArgumentNullException>().WithParameterName("attributes");
    }
}
