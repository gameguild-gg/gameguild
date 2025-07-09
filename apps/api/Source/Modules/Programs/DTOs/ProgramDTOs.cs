using GameGuild.Common.Domain.Enums;
using GameGuild.Modules.Contents.Models;


namespace GameGuild.Modules.Programs.DTOs;

// Program Management DTOs
public record CreateProgramDto(string Title, string? Description, string Slug, string? Thumbnail = null) {
  public string Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string Slug { get; init; } = Slug;

  public string? Thumbnail { get; init; } = Thumbnail;
}

public record UpdateProgramDto(string? Title = null, string? Description = null, string? Thumbnail = null) {
  public string? Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string? Thumbnail { get; init; } = Thumbnail;
}

public record CloneProgramDto(string NewTitle, string? NewDescription = null) {
  public string NewTitle { get; init; } = NewTitle;

  public string? NewDescription { get; init; } = NewDescription;
}

// Content Management DTOs
public record CreateContentDto(
  string Title,
  string Description,
  ProgramContentType Type,
  string Body,
  int? SortOrder = null,
  bool IsRequired = true,
  int? EstimatedMinutes = null
) {
  public string Title { get; init; } = Title;

  public string Description { get; init; } = Description;

  public ProgramContentType Type { get; init; } = Type;

  public string Body { get; init; } = Body;

  public int? SortOrder { get; init; } = SortOrder;

  public bool IsRequired { get; init; } = IsRequired;

  public int? EstimatedMinutes { get; init; } = EstimatedMinutes;
}

public record UpdateContentDto(
  string? Title = null,
  string? Description = null,
  string? Body = null,
  int? SortOrder = null,
  bool? IsRequired = null,
  int? EstimatedMinutes = null
) {
  public string? Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string? Body { get; init; } = Body;

  public int? SortOrder { get; init; } = SortOrder;

  public bool? IsRequired { get; init; } = IsRequired;

  public int? EstimatedMinutes { get; init; } = EstimatedMinutes;
}

public record ReorderContentDto(List<Guid> ContentIds) {
  public List<Guid> ContentIds { get; init; } = ContentIds;
}

// Workflow DTOs
public record RejectProgramDto(string Reason) {
  public string Reason { get; init; } = Reason;
}

public record SchedulePublishDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}

public record SetVisibilityDto(AccessLevel Visibility) {
  public AccessLevel Visibility { get; init; } = Visibility;
}

// User Progress DTOs
public record UpdateProgressDto(
  ProgressStatus? Status = null,
  DateTime? LastAccessedAt = null,
  Dictionary<string, object>? AdditionalData = null
) {
  public ProgressStatus? Status { get; init; } = Status;

  public DateTime? LastAccessedAt { get; init; } = LastAccessedAt;

  public Dictionary<string, object>? AdditionalData { get; init; } = AdditionalData;
}

public record UserProgressDto(
  decimal CompletionPercentage,
  DateTime? LastAccessedAt,
  DateTime? StartedAt,
  DateTime? CompletedAt,
  IEnumerable<ContentProgressDto> ContentProgress
) {
  public decimal CompletionPercentage { get; init; } = CompletionPercentage;

  public DateTime? LastAccessedAt { get; init; } = LastAccessedAt;

  public DateTime? StartedAt { get; init; } = StartedAt;

  public DateTime? CompletedAt { get; init; } = CompletedAt;

  public IEnumerable<ContentProgressDto> ContentProgress { get; init; } = ContentProgress;
}

public record ContentProgressDto(
  Guid ContentId,
  string Title,
  ProgressStatus Status,
  decimal CompletionPercentage,
  DateTime? FirstAccessedAt,
  DateTime? LastAccessedAt,
  DateTime? CompletedAt
) {
  public Guid ContentId { get; init; } = ContentId;

  public string Title { get; init; } = Title;

  public ProgressStatus Status { get; init; } = Status;

  public decimal CompletionPercentage { get; init; } = CompletionPercentage;

  public DateTime? FirstAccessedAt { get; init; } = FirstAccessedAt;

  public DateTime? LastAccessedAt { get; init; } = LastAccessedAt;

  public DateTime? CompletedAt { get; init; } = CompletedAt;
}

// Scheduling DTOs
public record ScheduleProgramDto(DateTime PublishAt) {
  public DateTime PublishAt { get; init; } = PublishAt;
}

// Monetization DTOs
public record MonetizationDto(
  decimal Price,
  string Currency = "USD",
  bool IsSubscription = false,
  int? SubscriptionDurationDays = null
) {
  public decimal Price { get; init; } = Price;

  public string Currency { get; init; } = Currency;

  public bool IsSubscription { get; init; } = IsSubscription;

  public int? SubscriptionDurationDays { get; init; } = SubscriptionDurationDays;
}

public record PricingDto(
  decimal Price,
  string Currency,
  bool IsSubscription,
  int? SubscriptionDurationDays,
  bool IsMonetizationEnabled
) {
  public decimal Price { get; init; } = Price;

  public string Currency { get; init; } = Currency;

  public bool IsSubscription { get; init; } = IsSubscription;

  public int? SubscriptionDurationDays { get; init; } = SubscriptionDurationDays;

  public bool IsMonetizationEnabled { get; init; } = IsMonetizationEnabled;
}

public record UpdatePricingDto(
  decimal? Price = null,
  string? Currency = null,
  bool? IsSubscription = null,
  int? SubscriptionDurationDays = null
) {
  public decimal? Price { get; init; } = Price;

  public string? Currency { get; init; } = Currency;

  public bool? IsSubscription { get; init; } = IsSubscription;

  public int? SubscriptionDurationDays { get; init; } = SubscriptionDurationDays;
}

// Product Integration DTOs
public record CreateProductFromProgramDto(string Name, string? Description, decimal BasePrice, string Currency = "USD") {
  public string Name { get; init; } = Name;

  public string? Description { get; init; } = Description;

  public decimal BasePrice { get; init; } = BasePrice;

  public string Currency { get; init; } = Currency;
}

// Analytics DTOs
public record ProgramAnalyticsDto(
  Guid ProgramId,
  string Title,
  int TotalUsers,
  int ActiveUsers,
  int CompletedUsers,
  decimal CompletionRate,
  TimeSpan AverageCompletionTime,
  int TotalViews,
  DateTime? LastActivity,
  Dictionary<string, object> AdditionalMetrics
) {
  public Guid ProgramId { get; init; } = ProgramId;

  public string Title { get; init; } = Title;

  public int TotalUsers { get; init; } = TotalUsers;

  public int ActiveUsers { get; init; } = ActiveUsers;

  public int CompletedUsers { get; init; } = CompletedUsers;

  public decimal CompletionRate { get; init; } = CompletionRate;

  public TimeSpan AverageCompletionTime { get; init; } = AverageCompletionTime;

  public int TotalViews { get; init; } = TotalViews;

  public DateTime? LastActivity { get; init; } = LastActivity;

  public Dictionary<string, object> AdditionalMetrics { get; init; } = AdditionalMetrics;
}

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

public record CompletionTrendDto(DateTime Date, int CompletedCount, int TotalCount, decimal Rate) {
  public DateTime Date { get; init; } = Date;

  public int CompletedCount { get; init; } = CompletedCount;

  public int TotalCount { get; init; } = TotalCount;

  public decimal Rate { get; init; } = Rate;
}

public record EngagementMetricsDto(
  Guid ProgramId,
  int DailyActiveUsers,
  int WeeklyActiveUsers,
  int MonthlyActiveUsers,
  TimeSpan AverageSessionDuration,
  int TotalSessions,
  decimal RetentionRate,
  Dictionary<string, int> ContentEngagement
) {
  public Guid ProgramId { get; init; } = ProgramId;

  public int DailyActiveUsers { get; init; } = DailyActiveUsers;

  public int WeeklyActiveUsers { get; init; } = WeeklyActiveUsers;

  public int MonthlyActiveUsers { get; init; } = MonthlyActiveUsers;

  public TimeSpan AverageSessionDuration { get; init; } = AverageSessionDuration;

  public int TotalSessions { get; init; } = TotalSessions;

  public decimal RetentionRate { get; init; } = RetentionRate;

  public Dictionary<string, int> ContentEngagement { get; init; } = ContentEngagement;
}

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

public record RevenueChartDto(DateTime Date, decimal Revenue, int Purchases) {
  public DateTime Date { get; init; } = Date;

  public decimal Revenue { get; init; } = Revenue;

  public int Purchases { get; init; } = Purchases;
}

// Search and Filter DTOs
public record ProgramSearchDto(
  string? SearchTerm = null,
  ContentStatus? Status = null,
  AccessLevel? Visibility = null,
  Guid? CreatorId = null,
  int Skip = 0,
  int Take = 50
) {
  public string? SearchTerm { get; init; } = SearchTerm;

  public ContentStatus? Status { get; init; } = Status;

  public AccessLevel? Visibility { get; init; } = Visibility;

  public Guid? CreatorId { get; init; } = CreatorId;

  public int Skip { get; init; } = Skip;

  public int Take { get; init; } = Take;
}

// Bulk Operations DTOs
public record BulkUpdateProgramsDto(
  List<Guid> ProgramIds,
  ContentStatus? Status = null,
  AccessLevel? Visibility = null
) {
  public List<Guid> ProgramIds { get; init; } = ProgramIds;

  public ContentStatus? Status { get; init; } = Status;

  public AccessLevel? Visibility { get; init; } = Visibility;
}

public record BulkAddUsersDto(Guid ProgramId, List<Guid> UserIds) {
  public Guid ProgramId { get; init; } = ProgramId;

  public List<Guid> UserIds { get; init; } = UserIds;
}

public record BulkRemoveUsersDto(Guid ProgramId, List<Guid> UserIds) {
  public Guid ProgramId { get; init; } = ProgramId;

  public List<Guid> UserIds { get; init; } = UserIds;
}
