using GameGuild.Modules.Programs;
using HotChocolate.Execution.Configuration;

namespace GameGuild.Source.Modules.Programs.GraphQL;

/// <summary> Extension methods for configuring Program GraphQL integration </summary>
public static class ProgramGraphQLExtensions {
    /// <summary> Configure GraphQL server with Program queries and mutations </summary>
    public static IRequestExecutorBuilder AddProgramGraphQL(this IRequestExecutorBuilder builder) {
        return builder
          .AddType<ProgramType>()
          .AddTypeExtension<ProgramQueries>()
          .AddTypeExtension<ProgramMutations>();
    }
}
