using HotChocolate.Execution.Configuration;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Extension methods for configuring ProgramContent GraphQL integration
/// </summary>
public static class ProgramContentGraphQLExtensions {
  /// <summary>
  /// Configure GraphQL server with DAC authorization for ProgramContent entity
  /// </summary>
  public static IRequestExecutorBuilder AddProgramContentGraphQL(this IRequestExecutorBuilder builder) {
    return builder.AddType<ProgramContentType>()
                  .AddTypeExtension<ProgramContentQueries>()
                  .AddTypeExtension<ProgramContentMutations>();
  }
}
