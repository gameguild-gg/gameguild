namespace GameGuild.Resources.Contents;

public interface IDocumentTemplateService
{
    Task<Result<IReadOnlyList<DocumentTemplate>>> ListAsync(
        string? category = null,
        string? supportedEntityType = null,
        CancellationToken ct = default);

    Task<Result<DocumentTemplate>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Result<DocumentTemplateCreatedResult>> CreateAsync(
        CreateDocumentTemplateInput input,
        Guid createdBy,
        CancellationToken ct = default);

    Task<Result<DocumentTemplate>> UpdateAsync(
        Guid id,
        UpdateDocumentTemplateInput input,
        CancellationToken ct = default);

    Task<Result<PublishedDocumentTemplateResult>> GetPublishedAsync(Guid id, CancellationToken ct = default);

    Task<Result<RenderedDocumentTemplateResult>> RenderPublishedAsync(
        Guid id,
        IReadOnlyDictionary<string, string?> variables,
        bool keepUnknownPlaceholders = false,
        CancellationToken ct = default);
}

public sealed record CreateDocumentTemplateInput(
    string TemplateKey,
    string Name,
    string? Description = null,
    string? Category = null,
    string? SupportedEntityType = null,
    string? PlaceholderSchema = null,
    string? InitialContent = null,
    bool IsSystemTemplate = false);

public sealed record UpdateDocumentTemplateInput(
    string Name,
    string? Description = null,
    string? Category = null,
    string? SupportedEntityType = null,
    string? PlaceholderSchema = null,
    bool? IsSystemTemplate = null);

public sealed record DocumentTemplateCreatedResult(DocumentTemplate Template, ContentVersion? DraftVersion);
public sealed record PublishedDocumentTemplateResult(DocumentTemplate Template, ContentVersion Version);
public sealed record RenderedDocumentTemplateResult(
    DocumentTemplate Template,
    ContentVersion Version,
    string? Content,
    IReadOnlyList<string> MissingVariables);
