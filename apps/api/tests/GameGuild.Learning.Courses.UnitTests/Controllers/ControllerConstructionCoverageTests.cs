using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Controllers;

public sealed class ControllerConstructionCoverageTests
{
    [Fact]
    public void Controllers_RequireTheirApplicationDependencies()
    {
        var sender = Mock.Of<ISender>();

        object[] controllers =
        [
            new ActivityGradeController(Mock.Of<IActivityGradeService>(), sender),
            new CourseStudentsController(sender, Mock.Of<IActorContextAccessor>()),
            new ProgramLifecycleController(sender),
        ];

        controllers.Should().OnlyContain(controller => controller != null);
    }
}
