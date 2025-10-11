using GameGuild.CQRS;
using GameGuild.Modules.TestingLab.Entities;


namespace GameGuild.Modules.TestingLab;

public record CreateTestingRequestCommand(
  Guid ProjectVersionId,
  string Title,
  string? Description,
  string? DownloadUrl,
  InstructionType InstructionsType,
  string? InstructionsContent,
  string? InstructionsUrl,
  Guid? InstructionsFileId,
  string? FeedbackFormContent,
  int? MaxTesters,
  DateTime StartDate,
  DateTime EndDate,
  bool IsActive = true
) : IRequest<TestingRequest>;
