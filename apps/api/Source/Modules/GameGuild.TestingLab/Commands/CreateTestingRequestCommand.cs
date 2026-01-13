using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.TestingLab;

/// <summary>
///     Command to create a new testing request
/// </summary>
[RequiresQuota(ResourceUsageType.TestingSessions, Source = "CreateTestingRequest")]
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
