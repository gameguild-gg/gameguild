using System.Security.Claims;
using GameGuild.Authorization;
using GameGuild.Authorization.Identity;
using GameGuild.CQRS;
using GameGuild.GraphQL;
using GameGuild.Modules.Programs;
using GameGuild.Source.Modules.Programs.Commands;
using GameGuild.Source.Modules.Programs.Models;
using Microsoft.Extensions.Logging;
using IMediator = GameGuild.CQRS.IMediator;
using ProgramEntity = GameGuild.Modules.Programs.Program;

namespace GameGuild.Source.Modules.Programs.GraphQL;

/// <summary> GraphQL mutations for Program module </summary>
[ExtendObjectType<Mutation>]
public class ProgramMutations {

    /// <summary> Creates a new program </summary>
    [HotChocolate.Authorization.Authorize]
    public async Task<ProgramEntity> CreateProgram(
      CreateProgramInput input,
      ClaimsPrincipal claimsPrincipal,
      [Service] IMediator mediator,
      [Service] IProgramService programService,
      [Service] ILogger<ProgramMutations> logger
    ) {
        logger.LogInformation("=== CreateProgram Debug ===");

        var userId = claimsPrincipal.GetUserId();
        if (userId == null) {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        var command = new CreateProgramCommand(
          Title: input.Title,
          Description: input.Description ?? "",
          Thumbnail: input.Thumbnail,
          EstimatedHours: input.EstimatedHours,
          Category: input.Category ?? ProgramCategory.Other,
          Difficulty: input.Difficulty ?? ProgramDifficulty.Beginner,
          VideoShowcaseUrl: input.VideoShowcaseUrl,
          CreatorId: userId?.ToString()
        );

        var program = await mediator.Send(command);
        logger.LogInformation("Created program with ID: {ProgramId}", program.Id);

        if (program == null) {
            throw new InvalidOperationException("Failed to retrieve created program");
        }

        logger.LogInformation("Successfully created program {ProgramId} for user {UserId}", program.Id, userId);
        return program;
    }

    /// <summary> Updates an existing program </summary>
    [GraphQLRequireResourcePermission<ProgramPermission, ProgramEntity>(PermissionType.Edit, "id")]
    public async Task<ProgramEntity> UpdateProgram(
      Guid id,
      UpdateProgramInput input,
      [Service] IMediator mediator,
      [Service] IProgramService programService,
      [Service] ILogger<ProgramMutations> logger
    ) {
        var command = new UpdateProgramCommand(
          Id: id,
          Title: input.Title,
          Description: input.Description,
          Category: input.Category,
          Difficulty: input.Difficulty,
          EstimatedHours: input.EstimatedHours,
          Thumbnail: input.Thumbnail,
          VideoShowcaseUrl: input.VideoShowcaseUrl
        );

        await mediator.Send(command);
        var program = await programService.GetProgramByIdAsync(id);

        if (program == null) {
            throw new InvalidOperationException("Failed to retrieve updated program");
        }

        logger.LogInformation("Successfully updated program {ProgramId}", id);
        return program;
    }

    /// <summary> Deletes a program </summary>
    [GraphQLRequireResourcePermission<ProgramPermission, ProgramEntity>(PermissionType.Delete, "id")]
    public async Task<bool> DeleteProgram(
      Guid id,
      [Service] IMediator mediator,
      [Service] ILogger<ProgramMutations> logger
    ) {
        var command = new DeleteProgramCommand(id);
        await mediator.Send(command);

        logger.LogInformation("Successfully deleted program {ProgramId}", id);
        return true;
    }

    /// <summary> Publishes a program (makes it publicly available) </summary>
    [GraphQLRequireResourcePermission<ProgramPermission, ProgramEntity>(PermissionType.Edit, "id")]
    public async Task<ProgramEntity> PublishProgram(
      Guid id,
      [Service] IProgramService programService,
      [Service] ILogger<ProgramMutations> logger
    ) {
        await programService.PublishAsync(id);
        var program = await programService.GetProgramByIdAsync(id);

        if (program == null) {
            throw new InvalidOperationException("Failed to retrieve published program");
        }

        logger.LogInformation("Successfully published program {ProgramId}", id);
        return program;
    }
}

/// <summary> Input type for creating a new program </summary>
public record CreateProgramInput(
  string Title,
  string? Description = null,
  ProgramCategory? Category = null,
  ProgramDifficulty? Difficulty = null,
  GameGuild.Modules.Contents.AccessLevel? Visibility = null,
  float? EstimatedHours = null,
  string? Thumbnail = null,
  string? VideoShowcaseUrl = null
);

/// <summary> Input type for updating a program </summary>
public record UpdateProgramInput(
  string? Title = null,
  string? Description = null,
  ProgramCategory? Category = null,
  ProgramDifficulty? Difficulty = null,
  GameGuild.Modules.Contents.AccessLevel? Visibility = null,
  float? EstimatedHours = null,
  string? Thumbnail = null,
  string? VideoShowcaseUrl = null
);
