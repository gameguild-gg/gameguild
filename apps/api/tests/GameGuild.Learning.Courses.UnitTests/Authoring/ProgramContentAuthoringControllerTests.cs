using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Authoring;

public sealed class ProgramContentAuthoringControllerTests
{
    [Fact]
    public void SerializeStreamEvent_UsesTheCamelCaseContractExpectedByTheWebClient()
    {
        var streamEvent = new AiStreamEvent(
            7,
            "delta",
            "Improved text",
            Guid.NewGuid(),
            "Running");

        var json = ProgramContentAuthoringController.SerializeStreamEvent(streamEvent);

        json.Should().Contain("\"sequence\":7");
        json.Should().Contain("\"delta\":\"Improved text\"");
        json.Should().NotContain("\"Sequence\"");
        json.Should().NotContain("\"Delta\"");
    }
}
