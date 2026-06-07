using System.Text.Json;
using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

public sealed class GetTenantMetadataQueryHandler(ITenantMetadataRepository repository)
    : IRequestHandler<GetTenantMetadataQuery, TenantMetadataDto?>
{
    public async Task<TenantMetadataDto?> Handle(GetTenantMetadataQuery request, CancellationToken cancellationToken)
    {
        var metadata = await repository.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false)
            ?? TenantMetadata.Create(request.TenantId);

        return TenantMetadataMapper.ToDto(metadata);
    }
}

public sealed class UpdateTenantMetadataCommandHandler(ITenantMetadataRepository repository)
    : IRequestHandler<UpdateTenantMetadataCommand>
{
    public async Task<Unit> Handle(UpdateTenantMetadataCommand request, CancellationToken cancellationToken)
    {
        var metadata = await TenantMetadataMapper.GetOrCreateMetadataAsync(repository, request.TenantId, cancellationToken).ConfigureAwait(false);
        TenantMetadataMapper.ApplyPartialUpdate(metadata, request.Request);
        await repository.UpdateAsync(metadata, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class ReplaceTenantMetadataCommandHandler(ITenantMetadataRepository repository)
    : IRequestHandler<ReplaceTenantMetadataCommand>
{
    public async Task<Unit> Handle(ReplaceTenantMetadataCommand request, CancellationToken cancellationToken)
    {
        var metadata = await TenantMetadataMapper.GetOrCreateMetadataAsync(repository, request.TenantId, cancellationToken).ConfigureAwait(false);
        TenantMetadataMapper.ApplyReplacement(metadata, request.Request);
        await repository.UpdateAsync(metadata, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class GetTenantCustomFieldsQueryHandler(ITenantMetadataRepository repository)
    : IRequestHandler<GetTenantCustomFieldsQuery, Dictionary<string, object?>?>
{
    public async Task<Dictionary<string, object?>?> Handle(GetTenantCustomFieldsQuery request, CancellationToken cancellationToken)
    {
        var metadata = await repository.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return metadata?.GetCustomFields() ?? new Dictionary<string, object?>();
    }
}

public sealed class UpdateTenantCustomFieldsCommandHandler(ITenantMetadataRepository repository)
    : IRequestHandler<UpdateTenantCustomFieldsCommand>
{
    public async Task<Unit> Handle(UpdateTenantCustomFieldsCommand request, CancellationToken cancellationToken)
    {
        var metadata = await TenantMetadataMapper.GetOrCreateMetadataAsync(repository, request.TenantId, cancellationToken).ConfigureAwait(false);
        metadata.SetCustomFields(TenantMetadataMapper.Merge(metadata.GetCustomFields(), request.Request.CustomFields));
        await repository.UpdateAsync(metadata, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class GetTenantTagsQueryHandler(ITenantMetadataRepository repository)
    : IRequestHandler<GetTenantTagsQuery, List<string>?>
{
    public async Task<List<string>?> Handle(GetTenantTagsQuery request, CancellationToken cancellationToken)
    {
        var metadata = await repository.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return metadata?.GetTags() ?? new List<string>();
    }
}

public sealed class UpdateTenantTagsCommandHandler(ITenantMetadataRepository repository)
    : IRequestHandler<UpdateTenantTagsCommand>
{
    public async Task<Unit> Handle(UpdateTenantTagsCommand request, CancellationToken cancellationToken)
    {
        var metadata = await TenantMetadataMapper.GetOrCreateMetadataAsync(repository, request.TenantId, cancellationToken).ConfigureAwait(false);
        metadata.SetTags(TenantMetadataMapper.MergeTags(metadata.GetTags(), request.Request.Tags));
        await repository.UpdateAsync(metadata, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class ReplaceTenantTagsCommandHandler(ITenantMetadataRepository repository)
    : IRequestHandler<ReplaceTenantTagsCommand>
{
    public async Task<Unit> Handle(ReplaceTenantTagsCommand request, CancellationToken cancellationToken)
    {
        var metadata = await TenantMetadataMapper.GetOrCreateMetadataAsync(repository, request.TenantId, cancellationToken).ConfigureAwait(false);
        metadata.SetTags(TenantMetadataMapper.NormalizeTags(request.Request.Tags));
        await repository.UpdateAsync(metadata, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

internal static class TenantMetadataMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<TenantMetadata> GetOrCreateMetadataAsync(ITenantMetadataRepository repository, Guid tenantId, CancellationToken ct)
    {
        var metadata = await repository.GetByTenantIdAsync(tenantId, ct).ConfigureAwait(false);
        if (metadata is not null)
            return metadata;

        metadata = TenantMetadata.Create(tenantId);
        await repository.AddAsync(metadata, ct).ConfigureAwait(false);
        await repository.SaveChangesAsync(ct).ConfigureAwait(false);

        return metadata;
    }

    public static TenantMetadataDto ToDto(TenantMetadata metadata)
    {
        return new TenantMetadataDto(
            metadata.TenantId,
            metadata.GetCustomFields(),
            metadata.GetTags(),
            metadata.GetExternalReferences(),
            ToBusinessInfo(metadata),
            ReadJson(metadata.ContactInfo, new TenantContactInfoDto(null, null, null, null, null, null)),
            metadata.Notes,
            ToOffset(metadata.CreatedAt),
            ToOffset(metadata.UpdatedAt)
        );
    }

    public static void ApplyPartialUpdate(TenantMetadata metadata, UpdateTenantMetadataRequest request)
    {
        if (request.CustomFields is not null)
            metadata.SetCustomFields(Merge(metadata.GetCustomFields(), request.CustomFields));

        if (request.Tags is not null)
            metadata.SetTags(MergeTags(metadata.GetTags(), request.Tags));

        if (request.ExternalReferences is not null)
            metadata.SetExternalReferences(Merge(metadata.GetExternalReferences(), request.ExternalReferences));

        if (request.BusinessInfo is not null)
            SetBusinessInfo(metadata, MergeBusinessInfo(ToBusinessInfo(metadata), request.BusinessInfo));

        if (request.ContactInfo is not null)
            metadata.ContactInfo = WriteJson(MergeContactInfo(ReadJson(metadata.ContactInfo, new TenantContactInfoDto(null, null, null, null, null, null)), request.ContactInfo));

        if (request.AdminNotes is not null)
            metadata.UpdateNotes(request.AdminNotes);

        metadata.Touch();
    }

    public static void ApplyReplacement(TenantMetadata metadata, ReplaceTenantMetadataRequest request)
    {
        metadata.SetCustomFields(new Dictionary<string, object?>(request.CustomFields));
        metadata.SetTags(NormalizeTags(request.Tags));
        metadata.SetExternalReferences(new Dictionary<string, string>(request.ExternalReferences));
        SetBusinessInfo(metadata, MergeBusinessInfo(new TenantBusinessInfoDto(null, null, null, null, new List<string>()), request.BusinessInfo));
        metadata.ContactInfo = WriteJson(MergeContactInfo(new TenantContactInfoDto(null, null, null, null, null, null), request.ContactInfo));
        metadata.UpdateNotes(request.AdminNotes);
        metadata.Touch();
    }

    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(Dictionary<TKey, TValue> current, Dictionary<TKey, TValue> update)
        where TKey : notnull
    {
        var merged = new Dictionary<TKey, TValue>(current);
        foreach (var item in update)
            merged[item.Key] = item.Value;

        return merged;
    }

    public static List<string> MergeTags(List<string> current, List<string> update)
        => NormalizeTags(current.Concat(update));

    public static List<string> NormalizeTags(IEnumerable<string> tags)
        => tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static TenantBusinessInfoDto ToBusinessInfo(TenantMetadata metadata)
    {
        var businessInfo = ReadJson(metadata.BusinessInfo, new TenantBusinessInfoDto(null, null, null, null, new List<string>()));
        return businessInfo with
        {
            Industry = businessInfo.Industry ?? metadata.Industry,
            OrganizationSize = businessInfo.OrganizationSize ?? metadata.Size?.ToString(),
            TenantType = businessInfo.TenantType ?? metadata.Type,
            ComplianceRequirements = businessInfo.ComplianceRequirements ?? new List<string>()
        };
    }

    private static TenantBusinessInfoDto MergeBusinessInfo(TenantBusinessInfoDto current, UpdateTenantBusinessInfoRequest update)
        => new(
            update.Industry ?? current.Industry,
            update.OrganizationSize ?? current.OrganizationSize,
            update.TenantType ?? current.TenantType,
            update.GeographicRegion ?? current.GeographicRegion,
            update.ComplianceRequirements ?? current.ComplianceRequirements
        );

    private static TenantContactInfoDto MergeContactInfo(TenantContactInfoDto current, UpdateTenantContactInfoRequest update)
        => new(
            update.PrimaryContactName ?? current.PrimaryContactName,
            update.PrimaryContactEmail ?? current.PrimaryContactEmail,
            update.PrimaryContactPhone ?? current.PrimaryContactPhone,
            update.OrganizationName ?? current.OrganizationName,
            update.Address is null
                ? current.Address
                : new TenantAddressDto(
                    update.Address.Street ?? current.Address?.Street,
                    update.Address.City ?? current.Address?.City,
                    update.Address.State ?? current.Address?.State,
                    update.Address.PostalCode ?? current.Address?.PostalCode,
                    update.Address.Country ?? current.Address?.Country
                ),
            update.Website ?? current.Website
        );

    private static void SetBusinessInfo(TenantMetadata metadata, TenantBusinessInfoDto businessInfo)
    {
        metadata.BusinessInfo = WriteJson(businessInfo);
        metadata.Industry = businessInfo.Industry;
        metadata.Type = businessInfo.TenantType;
        metadata.Size = Enum.TryParse<TenantSize>(businessInfo.OrganizationSize, ignoreCase: true, out var size)
            ? size
            : null;
    }

    private static T ReadJson<T>(string? json, T fallback)
    {
        if (string.IsNullOrWhiteSpace(json))
            return fallback;

        try { return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback; }
        catch (JsonException) { return fallback; }
    }

    private static string WriteJson<T>(T value)
        => JsonSerializer.Serialize(value, JsonOptions);

    private static DateTimeOffset ToOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return new DateTimeOffset(utc);
    }
}
