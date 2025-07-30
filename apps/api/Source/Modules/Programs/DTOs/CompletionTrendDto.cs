namespace GameGuild.Modules.Programs;

public record CompletionTrendDto(DateTime Date, int CompletedCount, int TotalCount, decimal Rate) {
  public DateTime Date { get; init; } = Date;

  public int CompletedCount { get; init; } = CompletedCount;

  public int TotalCount { get; init; } = TotalCount;

  public decimal Rate { get; init; } = Rate;
}
