using GameGuild.CQRS;


namespace GameGuild.TestingLab;

public sealed record UpdateTestingRequestCommand(
  Guid Id,
  string? Title,
  string? Description,
  string? DownloadUrl,
  InstructionType? InstructionsType,
  string? InstructionsContent,
  string? InstructionsUrl,
  Guid? InstructionsFileId,
  string? FeedbackFormContent,
  int? MaxTesters,
  DateTime? StartDate,
  DateTime? EndDate,
  bool? IsActive
) : IRequest<TestingRequest>;
