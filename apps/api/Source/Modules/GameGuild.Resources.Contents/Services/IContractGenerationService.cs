namespace GameGuild.Resources.Contents;

public interface IContractGenerationService
{
    Task<Result<GeneratedContractResult>> GenerateAsync(
        GenerateContractInput input,
        Guid createdBy,
        CancellationToken ct = default);
}

public sealed record GenerateContractInput(
    Guid DocumentTemplateId,
    string EntityType,
    Guid? EntityId,
    string Title,
    IReadOnlyDictionary<string, string?> Variables,
    string? Summary = null,
    bool Publish = false,
    bool AllowMissingVariables = false);

public sealed record GeneratedContractResult(
    Guid ContractId,
    Guid ContentVersionId,
    Guid DocumentTemplateId,
    string TemplateKey,
    string EntityType,
    Guid EntityId,
    int VersionNumber,
    string Title,
    string? Content,
    IReadOnlyList<string> MissingVariables,
    bool Published,
    DateTime GeneratedAtUtc);
