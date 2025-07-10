namespace GameGuild.Modules.Programs.DTOs;

public record RevenueAnalyticsDto(
  Guid ProgramId,
  decimal TotalRevenue,
  decimal MonthlyRevenue,
  int TotalPurchases,
  int MonthlyPurchases,
  decimal AverageRevenuePerUser,
  decimal ConversionRate,
  List<RevenueChartDto> RevenueChart
) {
  public Guid ProgramId { get; init; } = ProgramId;

  public decimal TotalRevenue { get; init; } = TotalRevenue;

  public decimal MonthlyRevenue { get; init; } = MonthlyRevenue;

  public int TotalPurchases { get; init; } = TotalPurchases;

  public int MonthlyPurchases { get; init; } = MonthlyPurchases;

  public decimal AverageRevenuePerUser { get; init; } = AverageRevenuePerUser;

  public decimal ConversionRate { get; init; } = ConversionRate;

  public List<RevenueChartDto> RevenueChart { get; init; } = RevenueChart;
}
