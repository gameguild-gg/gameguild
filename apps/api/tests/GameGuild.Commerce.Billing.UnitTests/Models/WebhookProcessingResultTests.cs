using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Models;

public class WebhookProcessingResultTests
{
    [Fact]
    public void Success_Should_Set_Processed_Fields()
    {
        var result = WebhookProcessingResult.Success("evt");

        result.Processed.Should().BeTrue();
        result.EventId.Should().Be("evt");
        result.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void AlreadyProcessed_Should_Set_Flags()
    {
        var processedAt = DateTime.UtcNow.AddMinutes(-1);
        var result = WebhookProcessingResult.AlreadyProcessed("evt", processedAt);

        result.WasAlreadyProcessed.Should().BeTrue();
        result.Processed.Should().BeTrue();
        result.ProcessedAt.Should().Be(processedAt);
    }

    [Fact]
    public void AlreadyProcessed_Should_Default_Timestamp_When_Null()
    {
        var result = WebhookProcessingResult.AlreadyProcessed("evt");

        result.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Failed_Should_Set_Error_Fields()
    {
        var result = WebhookProcessingResult.Failed("evt", "bad", requiresRetry: false);

        result.Processed.Should().BeFalse();
        result.ErrorMessage.Should().Be("bad");
        result.RequiresRetry.Should().BeFalse();
    }
}