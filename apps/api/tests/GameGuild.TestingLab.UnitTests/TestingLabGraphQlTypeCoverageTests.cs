using FluentAssertions;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabGraphQlTypeCoverageTests
{
    [Fact]
    public async Task RegisteredEntityTypes_BuildAnExecutableSchemaWithExpectedNames()
    {
        var services = new ServiceCollection();
        services
            .AddGraphQLServer()
            .AddQueryType<TestingLabTypeQuery>()
            .AddType<TestingRequestType>()
            .AddType<TestingSessionType>()
            .AddType<TestingParticipantType>()
            .AddType<TestingLocationType>();
        await using var provider = services.BuildServiceProvider();

        var executor = await provider
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();

        executor.Schema.GetType<HotChocolate.Types.ObjectType>("TestingRequest").Should().NotBeNull();
        executor.Schema.GetType<HotChocolate.Types.ObjectType>("TestingSession").Should().NotBeNull();
        executor.Schema.GetType<HotChocolate.Types.ObjectType>("TestingParticipant").Should().NotBeNull();
        executor.Schema.GetType<HotChocolate.Types.ObjectType>("TestingLocation").Should().NotBeNull();
    }

    public sealed class TestingLabTypeQuery
    {
        public TestingRequest? Request => null;
        public TestingSession? Session => null;
        public TestingParticipant? Participant => null;
        public TestingLocation? Location => null;
    }
}
