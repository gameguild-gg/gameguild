namespace GameGuild.Modules.Programs;

public record RevenueChartDto(DateTime Date, decimal Revenue, int Purchases) {
  public DateTime Date { get; init; } = Date;

  public decimal Revenue { get; init; } = Revenue;

  public int Purchases { get; init; } = Purchases;
}
