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
    public void SetActorContext_Should_Replace_Previous_Context()
    {
        var accessor = new ActorContextAccessor();
        var first = ActorContextBuilder.ForSystem("Job1").Build();
        var second = ActorContextBuilder.ForSystem("Job2").Build();

        accessor.SetActorContext(first);
        accessor.SetActorContext(second);

        accessor.ActorContext.Should().Be(second);
    }

    [Fact]
    public void SetActorContext_Should_Throw_When_Context_Null()
    {
        var accessor = new ActorContextAccessor();

        var act = () => accessor.SetActorContext(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
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
