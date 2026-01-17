using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Exceptions;

public class UnsupportedWebhookEventExceptionTests
{
    [Fact]
    public void Default_Constructor_Should_Set_Unknown_Type()
    {
        var ex = new UnsupportedWebhookEventException();

        ex.EventType.Should().Be("Unknown");
        ex.Message.Should().Contain("Unsupported webhook event type");
    }

    [Fact]
    public void Constructor_Should_Set_EventType_And_Message()
    {
        var ex = new UnsupportedWebhookEventException("evt");

        ex.EventType.Should().Be("evt");
        ex.Message.Should().Contain("evt");
    }

    [Fact]
    public void Constructor_Should_Set_InnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new UnsupportedWebhookEventException("evt", inner);

        ex.InnerException.Should().Be(inner);
    }
}