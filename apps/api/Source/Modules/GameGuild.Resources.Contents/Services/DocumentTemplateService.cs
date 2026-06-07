using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Resources.Contents;

public partial class DocumentTemplateService(IApplicationDbContext db) : IDocumentTemplateService
{
    public async Task<Result<IReadOnlyList<DocumentTemplate>>> ListAsync(
        string? category = null,
        string? supportedEntityType = null,
        CancellationToken ct = default)
    {
        var query = db.Set<DocumentTemplate>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(template => template.Category == category.Trim());
        }

        if (!string.IsNullOrWhiteSpace(supportedEntityType))
        {
            query = query.Where(template => template.SupportedEntityType == supportedEntityType.Trim());
        }

        var templates = await query
            .OrderBy(template => template.Category)
            .ThenBy(template => template.Name)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<DocumentTemplate>>(templates);
    }

    public async Task<Result<DocumentTemplate>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var template = await db.Set<DocumentTemplate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(current => current.Id == id, ct);

        return template is null
            ? Result.Failure<DocumentTemplate>(DocumentTemplateErrors.NotFound)
            : Result.Success(template);
    }

    public async Task<Result<DocumentTemplateCreatedResult>> CreateAsync(
        CreateDocumentTemplateInput input,
        Guid createdBy,
        CancellationToken ct = default)
    {
        var duplicate = await db.Set<DocumentTemplate>()
            .AnyAsync(template => template.TemplateKey == input.TemplateKey.Trim(), ct);

        if (duplicate)
        {
            return Result.Failure<DocumentTemplateCreatedResult>(DocumentTemplateErrors.DuplicateKey);
        }

        var template = DocumentTemplate.Create(
            input.TemplateKey,
            input.Name,
            input.Description,
            input.Category,
            input.SupportedEntityType,
            input.PlaceholderSchema,
            input.IsSystemTemplate);

        db.Set<DocumentTemplate>().Add(template);

        ContentVersion? draft = null;
        if (!string.IsNullOrWhiteSpace(input.InitialContent))
        {
            draft = ContentVersion.Create(
                template.Id,
                DocumentTemplate.VersionEntityType,
                1,
                template.Name,
                createdBy,
                body: input.InitialContent,
                changeNotes: "Initial document template draft.");

            db.Set<ContentVersion>().Add(draft);
        }

        await db.SaveChangesAsync(ct);

        return Result.Success(new DocumentTemplateCreatedResult(template, draft));
    }

    public async Task<Result<DocumentTemplate>> UpdateAsync(
        Guid id,
        UpdateDocumentTemplateInput input,
        CancellationToken ct = default)
    {
        var template = await db.Set<DocumentTemplate>().FirstOrDefaultAsync(current => current.Id == id, ct);
        if (template is null)
        {
            return Result.Failure<DocumentTemplate>(DocumentTemplateErrors.NotFound);
        }

        template.Update(
            input.Name,
            input.Description,
            input.Category,
            input.SupportedEntityType,
            input.PlaceholderSchema,
            input.IsSystemTemplate);

        await db.SaveChangesAsync(ct);

        return Result.Success(template);
    }

    public async Task<Result<PublishedDocumentTemplateResult>> GetPublishedAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var template = await db.Set<DocumentTemplate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(current => current.Id == id, ct);

        if (template is null)
        {
            return Result.Failure<PublishedDocumentTemplateResult>(DocumentTemplateErrors.NotFound);
        }

        var version = await db.Set<ContentVersion>()
            .AsNoTracking()
            .Where(current =>
                current.EntityId == id &&
                current.EntityType == DocumentTemplate.VersionEntityType &&
                current.Status == ContentVersionStatus.Published)
            .OrderByDescending(current => current.IsCurrentVersion)
            .ThenByDescending(current => current.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (version is null)
        {
            return Result.Failure<PublishedDocumentTemplateResult>(DocumentTemplateErrors.PublishedVersionNotFound);
        }

        return Result.Success(new PublishedDocumentTemplateResult(template, version));
    }

    public async Task<Result<RenderedDocumentTemplateResult>> RenderPublishedAsync(
        Guid id,
        IReadOnlyDictionary<string, string?> variables,
        bool keepUnknownPlaceholders = false,
        CancellationToken ct = default)
    {
        var published = await GetPublishedAsync(id, ct);
        if (published.IsFailure)
        {
            return Result.Failure<RenderedDocumentTemplateResult>(published.Error);
        }

        var normalizedVariables = new Dictionary<string, string?>(
            variables ?? new Dictionary<string, string?>(),
            StringComparer.OrdinalIgnoreCase);

        var missingVariables = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var content = PlaceholderRegex().Replace(published.Value.Version.Body ?? string.Empty, match =>
        {
            var key = match.Groups["key"].Value.Trim();
            if (normalizedVariables.TryGetValue(key, out var value) && value is not null)
            {
                return value;
            }

            missingVariables.Add(key);
            return keepUnknownPlaceholders ? match.Value : match.Value;
        });

        return Result.Success(new RenderedDocumentTemplateResult(
            published.Value.Template,
            published.Value.Version,
            content,
            missingVariables.ToList()));
    }

    [GeneratedRegex(@"\{\{\s*(?<key>[A-Za-z0-9_.-]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();
}

public static class DocumentTemplateErrors
{
    public static Error NotFound => Error.NotFound("DocumentTemplate.NotFound", "Document template not found");
    public static Error DuplicateKey => Error.Conflict("DocumentTemplate.DuplicateKey", "Document template key already exists");
    public static Error PublishedVersionNotFound => Error.NotFound("DocumentTemplate.PublishedVersionNotFound", "Published document template version not found");
}
