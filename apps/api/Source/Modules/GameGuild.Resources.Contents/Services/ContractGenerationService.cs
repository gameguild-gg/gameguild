using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Resources.Contents;

public sealed class ContractGenerationService(
    IApplicationDbContext db,
    IDocumentTemplateService documentTemplateService) : IContractGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<GeneratedContractResult>> GenerateAsync(
        GenerateContractInput input,
        Guid createdBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.EntityType))
        {
            return Result.Failure<GeneratedContractResult>(
                Error.Validation("ContractGeneration.EntityTypeRequired", "Entity type is required."));
        }

        if (string.IsNullOrWhiteSpace(input.Title))
        {
            return Result.Failure<GeneratedContractResult>(
                Error.Validation("ContractGeneration.TitleRequired", "Contract title is required."));
        }

        var render = await documentTemplateService.RenderPublishedAsync(
            input.DocumentTemplateId,
            input.Variables,
            input.AllowMissingVariables,
            ct).ConfigureAwait(false);

        if (render.IsFailure)
        {
            return Result.Failure<GeneratedContractResult>(render.Error);
        }

        if (!input.AllowMissingVariables && render.Value.MissingVariables.Count > 0)
        {
            return Result.Failure<GeneratedContractResult>(
                Error.Validation(
                    "ContractGeneration.MissingVariables",
                    $"Missing template variables: {string.Join(", ", render.Value.MissingVariables)}"));
        }

        var entityType = input.EntityType.Trim();
        var entityId = input.EntityId ?? Guid.NewGuid();
        var nextVersion = await db.Set<ContentVersion>()
            .Where(version => version.EntityId == entityId && version.EntityType == entityType)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(ct).ConfigureAwait(false) ?? 0;

        var metadata = JsonSerializer.Serialize(new
        {
            contractGenerated = true,
            generatedAtUtc = SystemClock.UtcNow,
            documentTemplateId = input.DocumentTemplateId,
            templateKey = render.Value.Template.TemplateKey,
            templateVersionId = render.Value.Version.Id,
            variables = input.Variables,
            missingVariables = render.Value.MissingVariables
        }, JsonOptions);

        var contentVersion = ContentVersion.Create(
            entityId,
            entityType,
            nextVersion + 1,
            input.Title,
            createdBy,
            input.Summary,
            render.Value.Content,
            metadata,
            $"Generated from document template {render.Value.Template.TemplateKey}.");

        if (input.Publish)
        {
            var currentPublished = await db.Set<ContentVersion>()
                .Where(version =>
                    version.EntityId == entityId &&
                    version.EntityType == entityType &&
                    version.Status == ContentVersionStatus.Published)
                .ToListAsync(ct).ConfigureAwait(false);

            foreach (var current in currentPublished)
            {
                current.Archive();
            }

            contentVersion.SubmitForReview(createdBy);
            contentVersion.Approve(createdBy, "Generated contract auto-approved for publication.");
            contentVersion.Publish(createdBy);
        }

        db.Set<ContentVersion>().Add(contentVersion);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success(new GeneratedContractResult(
            contentVersion.EntityId,
            contentVersion.Id,
            input.DocumentTemplateId,
            render.Value.Template.TemplateKey,
            contentVersion.EntityType,
            contentVersion.EntityId,
            contentVersion.VersionNumber,
            contentVersion.Title,
            contentVersion.Body,
            render.Value.MissingVariables,
            contentVersion.Status == ContentVersionStatus.Published,
            contentVersion.CreatedAt));
    }
}
