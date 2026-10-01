using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GameGuild.API.Core.OpenApi;

/// <summary>
///     Applies configured locale-specific text without changing the base Swagger document.
/// </summary>
internal sealed class OpenApiLocalizedDocumentFilter(OpenApiLocalizationOptions options) : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        var locale = options.GetNormalizedLocales().FirstOrDefault(pair =>
            context.DocumentName.EndsWith($".{pair.Key}", StringComparison.OrdinalIgnoreCase));
        if (locale.Value is null)
            return;

        ApplyInfo(document.Info, locale.Value);
        ApplyTags(document, locale.Value.Tags);
        ApplyOperations(document, locale.Value.Operations);
        ApplySchemas(document, locale.Value.Schemas);
    }

    private static void ApplyInfo(OpenApiInfo info, OpenApiLocalizedDocumentOptions translations)
    {
        if (translations.Title is not null)
            info.Title = translations.Title;

        if (translations.Description is not null)
            info.Description = translations.Description;
    }

    private static void ApplyTags(OpenApiDocument document, IReadOnlyDictionary<string, string> translations)
    {
        if (translations.Count == 0)
            return;

        var usedTags = document.Paths.Values
            .SelectMany(path => path.Operations.Values)
            .SelectMany(operation => operation.Tags)
            .Select(tag => tag.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        document.Tags ??= [];
        foreach (var (name, description) in translations.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!usedTags.Contains(name))
                continue;

            var documentTag = document.Tags.FirstOrDefault(tag =>
                string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase));
            if (documentTag is null)
            {
                documentTag = new OpenApiTag { Name = name };
                document.Tags.Add(documentTag);
            }

            documentTag.Description = description;
            foreach (var tag in document.Paths.Values
                         .SelectMany(path => path.Operations.Values)
                         .SelectMany(operation => operation.Tags)
                         .Where(tag => string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                tag.Description = description;
            }
        }
    }

    private static void ApplyOperations(
        OpenApiDocument document,
        IReadOnlyDictionary<string, OpenApiLocalizedOperationOptions> translations)
    {
        if (translations.Count == 0)
            return;

        foreach (var (path, pathItem) in document.Paths)
        foreach (var (method, operation) in pathItem.Operations)
        {
            var key = operation.OperationId;
            if (!TryGetValueIgnoreCase(translations, key, out var translation))
            {
                key = $"{method.ToString().ToUpperInvariant()} {path}";
                if (!TryGetValueIgnoreCase(translations, key, out translation))
                    continue;
            }

            if (translation.Summary is not null)
                operation.Summary = translation.Summary;

            if (translation.Description is not null)
                operation.Description = translation.Description;
        }
    }

    private static void ApplySchemas(
        OpenApiDocument document,
        IReadOnlyDictionary<string, OpenApiLocalizedSchemaOptions> translations)
    {
        if (translations.Count == 0 || document.Components?.Schemas is not { } schemas)
            return;

        foreach (var (schemaId, translation) in translations)
        {
            if (!TryGetValueIgnoreCase(schemas, schemaId, out var schema))
                continue;

            if (translation.Description is not null)
                schema.Description = translation.Description;

            foreach (var (propertyName, description) in translation.Properties)
            {
                if (TryGetValueIgnoreCase(schema.Properties, propertyName, out var property))
                    property.Description = description;
            }
        }
    }

    private static bool TryGetValueIgnoreCase<TValue>(
        IEnumerable<KeyValuePair<string, TValue>> values,
        string? key,
        out TValue value)
    {
        if (key is not null)
        {
            foreach (var pair in values)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }
        }

        value = default!;
        return false;
    }
}
