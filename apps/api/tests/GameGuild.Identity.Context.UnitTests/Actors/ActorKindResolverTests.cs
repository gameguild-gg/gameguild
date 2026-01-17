using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class ActorKindResolverTests
{
    [Fact]
    public void Resolve_Should_Prioritize_GrantType()
    {
        var kind = ActorKindResolver.Resolve("client_credentials", actorTypeClaim: "user", subjectId: null);

        kind.Should().Be(ActorKind.Service);
    }

    [Fact]
    public void Resolve_Should_Use_ActorType_Claim_When_No_GrantType()
    {
        var kind = ActorKindResolver.Resolve(null, actorTypeClaim: "system", subjectId: null);

        kind.Should().Be(ActorKind.System);
    }

    [Fact]
    public void Resolve_Should_Use_SubjectId_When_No_GrantType_Or_Claim()
    {
        var kind = ActorKindResolver.Resolve(null, null, SystemActor.SystemSubjectIdConstant);

        kind.Should().Be(ActorKind.System);
    }

    [Fact]
    public void Resolve_Should_Default_To_User()
    {
        var kind = ActorKindResolver.Resolve(null, null, null);

        kind.Should().Be(ActorKind.User);
    }
}
