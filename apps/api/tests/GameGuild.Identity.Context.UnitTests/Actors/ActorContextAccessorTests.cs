using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class ActorContextAccessorTests
{
    [Fact]
    public void ActorContext_Should_Default_To_Anonymous()
    {
        var accessor = new ActorContextAccessor();

        accessor.ActorContext.Should().Be(ActorContext.Anonymous);
    }

    [Fact]
    public void SetActorContext_Should_Store_Context()
    {
        var accessor = new ActorContextAccessor();
        var context = ActorContextBuilder.ForSystem("Job").Build();

        accessor.SetActorContext(context);

        accessor.ActorContext.Should().Be(context);
    }

    [Fact]
    public void ClearActorContext_Should_Reset_To_Anonymous()
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(ActorContextBuilder.ForSystem("Job").Build());

        accessor.ClearActorContext();

        accessor.ActorContext.Should().Be(ActorContext.Anonymous);
    }
}
