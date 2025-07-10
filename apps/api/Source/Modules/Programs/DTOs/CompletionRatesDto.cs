namespace GameGuild.Modules.Programs.DTOs;

public record CompletionRatesDto(
  Guid ProgramId,
  decimal OverallCompletionRate,
  Dictionary<Guid, decimal> ContentCompletionRates,
  List<CompletionTrendDto> CompletionTrends
) {
  public Guid ProgramId { get; init; } = ProgramId;

  public decimal OverallCompletionRate { get; init; } = OverallCompletionRate;

  public Dictionary<Guid, decimal> ContentCompletionRates { get; init; } = ContentCompletionRates;

  public List<CompletionTrendDto> CompletionTrends { get; init; } = CompletionTrends;
}
