namespace GameGuild.Analytics;

public sealed record DashboardWidgetDto(
    Guid Id,
    string Title,
    WidgetType Type,
    int SortOrder,
    string? Configuration);

public sealed record DashboardDto(
    Guid Id,
    Guid? TenantId,
    string Title,
    string Slug,
    string? Description,
    bool IsDefault,
    IReadOnlyList<DashboardWidgetDto> Widgets,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record DashboardWidgetRequest(
    string Title,
    WidgetType Type,
    int SortOrder,
    string? Configuration = null);

public sealed record CreateDashboardRequest(
    string Title,
    string Slug,
    string? Description = null,
    bool IsDefault = false,
    Guid? TenantId = null,
    IReadOnlyList<DashboardWidgetRequest>? Widgets = null);

public sealed record UpdateDashboardRequest(
    string Title,
    string Slug,
    string? Description = null,
    bool IsDefault = false,
    IReadOnlyList<DashboardWidgetRequest>? Widgets = null);
