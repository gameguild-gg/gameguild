using HotChocolate.Execution.Configuration;

namespace GameGuild.Modules.Program.GraphQL;

/// <summary>
/// Extension methods for configuring ContentInteraction GraphQL integration
/// </summary>
public static class ContentInteractionGraphQLExtensions
{
    /// <summary>
    /// Configure GraphQL server with DAC authorization for ContentInteraction entity
    /// </summary>
    public static IRequestExecutorBuilder AddContentInteractionGraphQL(this IRequestExecutorBuilder builder)
    {
        return builder.AddType<ContentInteractionType>()
                      .AddTypeExtension<ContentInteractionQueries>()
                      .AddTypeExtension<ContentInteractionMutations>();
    }
}
