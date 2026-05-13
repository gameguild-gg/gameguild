using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class ActorTypesTests
{
    [Fact]
    public void ServiceActor_Should_Expose_Expected_Properties()
    {
        var actor = new ServiceActor("svc", "Service", new HashSet<string> { "scope" });

        actor.Kind.Should().Be(ActorKind.Service);
        actor.SubjectId.Should().Be("svc");
        actor.DisplayName.Should().Be("Service");
        actor.Scopes.Should().Contain("scope");
    }

    [Fact]
    public void ServiceActor_Should_Default_Scopes_When_Not_Provided()
    {
        var actor = new ServiceActor("svc", "Service");

        actor.Scopes.Should().NotBeNull();
        actor.Scopes.Should().BeEmpty();
    }

    [Fact]
    public void ServiceActor_Should_Throw_When_ServiceId_Null()
    {
        var act = () => new ServiceActor(null!, "Service");

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceId");
    }

    [Fact]
    public void ServiceActor_Should_Throw_When_ServiceName_Null()
    {
        var act = () => new ServiceActor("svc", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceName");
    }

    [Fact]
    public void UserActor_FromSubject_Should_Parse_Guid()
    {
        var id = Guid.NewGuid();

        var actor = UserActor.FromSubject(id.ToString(), "user@example.com", "User");

        actor.UserId.Should().Be(id);
        actor.DisplayName.Should().Be("User");
    }

    [Fact]
    public void UserActor_FromSubject_Should_Throw_On_Invalid_Guid()
    {
        var act = () => UserActor.FromSubject("not-a-guid");

        act.Should().Throw<ArgumentException>().WithParameterName("subjectId");
    }

    [Fact]
    public void UserActor_DisplayName_Should_Fallback_To_Email()
    {
        var id = Guid.NewGuid();
        var actor = new UserActor(id, "user@example.com", null);

        actor.DisplayName.Should().Be("user@example.com");
    }

    [Fact]
    public void UserActor_Should_Expose_Kind_SubjectId_And_Id_Fallback_DisplayName()
    {
        var id = Guid.NewGuid();
        var actor = new UserActor(id);

        actor.Kind.Should().Be(ActorKind.User);
        actor.SubjectId.Should().Be(id.ToString());
        actor.DisplayName.Should().Be(id.ToString());
    }

    [Fact]
    public void SystemActor_Factories_Should_Prefix_OperationNames()
    {
        SystemActor.ForBackgroundJob("Sync").DisplayName.Should().Be("BackgroundJob:Sync");
        SystemActor.ForScheduler("Hourly").DisplayName.Should().Be("Scheduler:Hourly");
        SystemActor.ForMigration("Init").DisplayName.Should().Be("Migration:Init");
        SystemActor.ForSeeding().DisplayName.Should().Be("Seeding");
    }

    [Fact]
    public void SystemActor_Should_Expose_System_Identity_Metadata()
    {
        var actor = new SystemActor("NightlyJob", "corr-1");

        SystemActor.SystemSubjectIdConstant.Should().Be("system");
        SystemActor.SystemSubjectId.Should().Be(SystemActor.SystemSubjectIdConstant);
        actor.Kind.Should().Be(ActorKind.System);
        actor.SubjectId.Should().Be("system");
        actor.DisplayName.Should().Be("NightlyJob");
        actor.CorrelationId.Should().Be("corr-1");
    }
}
