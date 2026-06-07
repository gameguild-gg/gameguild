using GameGuild.CQRS;

namespace GameGuild.Analytics;

public sealed record GetDashboardsQuery(Guid? TenantId = null) : IQuery<IReadOnlyList<DashboardDto>>;

public sealed record GetDashboardByIdQuery(Guid Id) : IQuery<DashboardDto?>;

public sealed record CreateDashboardCommand(CreateDashboardRequest Request) : ICommand<DashboardDto>;

public sealed record UpdateDashboardCommand(Guid Id, UpdateDashboardRequest Request) : ICommand<DashboardDto?>;

public sealed class GetDashboardsQueryHandler(IDashboardRepository repository)
    : IQueryHandler<GetDashboardsQuery, IReadOnlyList<DashboardDto>>
{
    public async Task<IReadOnlyList<DashboardDto>> Handle(GetDashboardsQuery request, CancellationToken cancellationToken)
        => (await repository.GetAllAsync(request.TenantId, cancellationToken).ConfigureAwait(false))
            .OrderByDescending(dashboard => dashboard.IsDefault)
            .ThenBy(dashboard => dashboard.Title)
            .Select(DashboardMapping.ToDto)
            .ToList();
}

public sealed class GetDashboardByIdQueryHandler(IDashboardRepository repository)
    : IQueryHandler<GetDashboardByIdQuery, DashboardDto?>
{
    public async Task<DashboardDto?> Handle(GetDashboardByIdQuery request, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false) is { } dashboard
            ? DashboardMapping.ToDto(dashboard)
            : null;
}

public sealed class CreateDashboardCommandHandler(IDashboardRepository repository)
    : ICommandHandler<CreateDashboardCommand, DashboardDto>
{
    public async Task<DashboardDto> Handle(CreateDashboardCommand request, CancellationToken cancellationToken)
    {
        DashboardCommandHelpers.Validate(request.Request.Title, request.Request.Slug);

        var existing = await repository.GetBySlugAsync(DashboardCommandHelpers.NormalizeSlug(request.Request.Slug), cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Dashboard slug '{request.Request.Slug}' already exists.");
        }

        var dashboard = new Dashboard
        {
            Title = request.Request.Title.Trim(),
            Slug = DashboardCommandHelpers.NormalizeSlug(request.Request.Slug),
            Description = DashboardCommandHelpers.NormalizeOptional(request.Request.Description),
            IsDefault = request.Request.IsDefault,
        };

        if (request.Request.TenantId.HasValue)
        {
            dashboard.SetTenantId(request.Request.TenantId.Value);
        }

        DashboardCommandHelpers.ReplaceWidgets(dashboard, request.Request.Widgets);

        var created = await repository.AddAsync(dashboard, cancellationToken).ConfigureAwait(false);
        return DashboardMapping.ToDto(created);
    }
}

public sealed class UpdateDashboardCommandHandler(IDashboardRepository repository)
    : ICommandHandler<UpdateDashboardCommand, DashboardDto?>
{
    public async Task<DashboardDto?> Handle(UpdateDashboardCommand request, CancellationToken cancellationToken)
    {
        DashboardCommandHelpers.Validate(request.Request.Title, request.Request.Slug);

        var dashboard = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (dashboard is null)
        {
            return null;
        }

        var normalizedSlug = DashboardCommandHelpers.NormalizeSlug(request.Request.Slug);
        var existing = await repository.GetBySlugAsync(normalizedSlug, cancellationToken).ConfigureAwait(false);
        if (existing is not null && existing.Id != dashboard.Id)
        {
            throw new InvalidOperationException($"Dashboard slug '{request.Request.Slug}' already exists.");
        }

        dashboard.Title = request.Request.Title.Trim();
        dashboard.Slug = normalizedSlug;
        dashboard.Description = DashboardCommandHelpers.NormalizeOptional(request.Request.Description);
        dashboard.IsDefault = request.Request.IsDefault;

        if (request.Request.Widgets is not null)
        {
            DashboardCommandHelpers.ReplaceWidgets(dashboard, request.Request.Widgets);
        }

        await repository.UpdateAsync(dashboard, cancellationToken).ConfigureAwait(false);
        return DashboardMapping.ToDto(dashboard);
    }
}

internal static class DashboardMapping
{
    public static DashboardDto ToDto(Dashboard dashboard)
        => new(
            dashboard.Id,
            dashboard.TenantId,
            dashboard.Title,
            dashboard.Slug,
            dashboard.Description,
            dashboard.IsDefault,
            dashboard.Widgets
                .Where(widget => !widget.IsDeleted)
                .OrderBy(widget => widget.SortOrder)
                .Select(widget => new DashboardWidgetDto(
                    widget.Id,
                    widget.Title,
                    widget.Type,
                    widget.SortOrder,
                    widget.Configuration))
                .ToList(),
            dashboard.CreatedAt,
            dashboard.UpdatedAt);
}

file static class DashboardCommandHelpers
{
    public static void Validate(string title, string slug)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Dashboard title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Dashboard slug is required.", nameof(slug));
        }
    }

    public static void ReplaceWidgets(Dashboard dashboard, IReadOnlyList<DashboardWidgetRequest>? widgets)
    {
        dashboard.Widgets.Clear();

        foreach (var widget in widgets ?? [])
        {
            if (string.IsNullOrWhiteSpace(widget.Title))
            {
                throw new ArgumentException("Dashboard widget title is required.", nameof(widgets));
            }

            dashboard.Widgets.Add(new DashboardWidget
            {
                DashboardId = dashboard.Id,
                Title = widget.Title.Trim(),
                Type = widget.Type,
                SortOrder = widget.SortOrder,
                Configuration = NormalizeOptional(widget.Configuration),
                TenantId = dashboard.TenantId,
            });
        }
    }

    public static string NormalizeSlug(string value)
        => value.Trim().ToLowerInvariant();

    public static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
