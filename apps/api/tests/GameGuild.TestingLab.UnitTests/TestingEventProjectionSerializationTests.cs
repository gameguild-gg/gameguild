using System.Text.Json;
using FluentAssertions;
using GameGuild.TestingLab;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingEventProjectionSerializationTests
{
    [Fact]
    public void BlankEventProjection_OmitsNullConfigurationFromPublicResponse()
    {
        var projection = new TestingEventProjection(
            Guid.NewGuid(),
            "Draft testing event",
            null,
            TestingEventMode.Online,
            TestingEventApprovalMode.ManagerOnly,
            TestingEventStatus.Draft,
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddHours(2),
            true,
            TestingLearningCompletionRequirement.None,
            null,
            null,
            null,
            Guid.NewGuid(),
            0,
            0);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(
            projection,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        document.RootElement.TryGetProperty("configuration", out _).Should().BeFalse(
            "a blank event has not yet configured its application or registration schemas");
    }
}
