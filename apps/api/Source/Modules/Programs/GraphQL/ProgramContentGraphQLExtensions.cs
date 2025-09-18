using GameGuild.Modules.Programs;
using HotChocolate.Execution.Configuration;
using ProgramContentTypeEnum = GameGuild.Source.Modules.Programs.Models.ProgramContentType;

namespace GameGuild.Source.Modules.Programs.GraphQL;

/// <summary> Extension methods for configuring ProgramContent GraphQL integration </summary>
public static class ProgramContentGraphQLExtensions {
  /// <summary> Configure GraphQL server with DAC authorization for ProgramContent entity </summary>
  public static IRequestExecutorBuilder AddProgramContentGraphQL(this IRequestExecutorBuilder builder) { return builder.AddType<ProgramContentTypeEnum>().AddTypeExtension<ProgramContentQueries>().AddTypeExtension<ProgramContentMutations>(); }
}
