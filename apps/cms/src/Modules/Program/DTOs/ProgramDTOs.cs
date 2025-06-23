using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Program.DTOs;

// Program Management DTOs
public record CreateProgramDto(
    string Title, 
    string? Description, 
    string Slug,
    string? Thumbnail = null
);

public record UpdateProgramDto(
    string? Title = null, 
    string? Description = null, 
    string? Thumbnail = null
);

public record CloneProgramDto(
    string NewTitle, 
    string? NewDescription = null
);

// Content Management DTOs
public record CreateContentDto(
    string Title, 
    string Description, 
    ProgramContentType Type, 
    string Body,
    int? SortOrder = null,
    bool IsRequired = true,
    int? EstimatedMinutes = null
);

public record UpdateContentDto(
    string? Title = null, 
    string? Description = null, 
    string? Body = null,
    int? SortOrder = null,
    bool? IsRequired = null,
    int? EstimatedMinutes = null
);

public record ReorderContentDto(
    List<Guid> ContentIds
);

// Workflow DTOs
public record RejectProgramDto(
    string Reason
);

public record SchedulePublishDto(
    DateTime PublishAt
);

public record SetVisibilityDto(
    AccessLevel Visibility
);

// User Progress DTOs
public record UpdateProgressDto(
    ProgressStatus? Status = null,
    DateTime? LastAccessedAt = null,
    Dictionary<string, object>? AdditionalData = null
);

public record UserProgressDto(
    decimal CompletionPercentage, 
    DateTime? LastAccessedAt, 
    DateTime? StartedAt,
    DateTime? CompletedAt,
    IEnumerable<ContentProgressDto> ContentProgress
);

public record ContentProgressDto(
    Guid ContentId, 
    string Title, 
    ProgressStatus Status, 
    decimal CompletionPercentage,
    DateTime? FirstAccessedAt,
    DateTime? LastAccessedAt,
    DateTime? CompletedAt
);

// Scheduling DTOs
public record ScheduleProgramDto(
    DateTime PublishAt
);

// Monetization DTOs
public record MonetizationDto(
    decimal Price,
    string Currency = "USD",
    bool IsSubscription = false,
    int? SubscriptionDurationDays = null
);

public record PricingDto(
    decimal Price,
    string Currency,
    bool IsSubscription,
    int? SubscriptionDurationDays,
    bool IsMonetizationEnabled
);

public record UpdatePricingDto(
    decimal? Price = null,
    string? Currency = null,
    bool? IsSubscription = null,
    int? SubscriptionDurationDays = null
);

// Product Integration DTOs
public record CreateProductFromProgramDto(
    string Name, 
    string? Description, 
    decimal BasePrice, 
    string Currency = "USD"
);

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
);

public record CompletionRatesDto(
    Guid ProgramId,
    decimal OverallCompletionRate,
    Dictionary<Guid, decimal> ContentCompletionRates,
    List<CompletionTrendDto> CompletionTrends
);

public record CompletionTrendDto(
    DateTime Date,
    int CompletedCount,
    int TotalCount,
    decimal Rate
);

public record EngagementMetricsDto(
    Guid ProgramId,
    int DailyActiveUsers,
    int WeeklyActiveUsers,
    int MonthlyActiveUsers,
    TimeSpan AverageSessionDuration,
    int TotalSessions,
    decimal RetentionRate,
    Dictionary<string, int> ContentEngagement
);

public record RevenueAnalyticsDto(
    Guid ProgramId,
    decimal TotalRevenue,
    decimal MonthlyRevenue,
    int TotalPurchases,
    int MonthlyPurchases,
    decimal AverageRevenuePerUser,
    decimal ConversionRate,
    List<RevenueChartDto> RevenueChart
);

public record RevenueChartDto(
    DateTime Date,
    decimal Revenue,
    int Purchases
);

// Search and Filter DTOs
public record ProgramSearchDto(
    string? SearchTerm = null,
    ContentStatus? Status = null,
    AccessLevel? Visibility = null,
    Guid? CreatorId = null,
    int Skip = 0,
    int Take = 50
);

// Bulk Operations DTOs
public record BulkUpdateProgramsDto(
    List<Guid> ProgramIds,
    ContentStatus? Status = null,
    AccessLevel? Visibility = null
);

public record BulkAddUsersDto(
    Guid ProgramId,
    List<Guid> UserIds
);

public record BulkRemoveUsersDto(
    Guid ProgramId,
    List<Guid> UserIds
);
