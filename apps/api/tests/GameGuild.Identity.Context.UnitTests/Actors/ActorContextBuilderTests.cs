using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class ActorContextBuilderTests
{
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
}
