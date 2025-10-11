namespace GameGuild.Modules.Search.Indexing;

/// <summary>
/// Search indexing service interface.
/// </summary>
public interface ISearchIndexService
{
    Task<IndexResult> IndexDocumentAsync(
        string indexName,
        string documentId,
        Dictionary<string, object> fields,
        CancellationToken cancellationToken = default);

    Task<BulkIndexResult> IndexDocumentsAsync(
        string indexName,
        IEnumerable<SearchDocument> documents,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(
        string indexName,
        string documentId,
        CancellationToken cancellationToken = default);

    Task<SearchResult> SearchAsync(
        string indexName,
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<SearchResult> FacetedSearchAsync(
        string indexName,
        string query,
        string[] facets,
        Dictionary<string, string[]>? filters = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> AutocompleteAsync(
        string indexName,
        string query,
        string fieldName,
        int maxSuggestions = 10,
        CancellationToken cancellationToken = default);

    Task<bool> CreateIndexAsync(
        string indexName,
        IndexSchema schema,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteIndexAsync(
        string indexName,
        CancellationToken cancellationToken = default);

    Task<IndexStatistics> GetStatisticsAsync(
        string indexName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Search indexing service implementation.
/// </summary>
public sealed class SearchIndexService : ISearchIndexService
{
    private readonly ILogger<SearchIndexService> _logger;
    private readonly Dictionary<string, SearchIndex> _indexes;
    private readonly Dictionary<string, IndexSchema> _schemas;

    public SearchIndexService(ILogger<SearchIndexService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _indexes = new Dictionary<string, SearchIndex>();
        _schemas = new Dictionary<string, IndexSchema>();
    }

    public Task<IndexResult> IndexDocumentAsync(
        string indexName,
        string documentId,
        Dictionary<string, object> fields,
        CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out var index))
        {
            throw new InvalidOperationException($"Index '{indexName}' does not exist");
        }

        var document = new SearchDocument
        {
            Id = documentId,
            Fields = fields,
            IndexedAt = DateTime.UtcNow
        };

        index.Documents[documentId] = document;
        index.TotalDocuments++;

        _logger.LogInformation("Indexed document {DocumentId} in index {IndexName}",
            documentId, indexName);

        return Task.FromResult(new IndexResult
        {
            Success = true,
            DocumentId = documentId,
            IndexName = indexName
        });
    }

    public Task<BulkIndexResult> IndexDocumentsAsync(
        string indexName,
        IEnumerable<SearchDocument> documents,
        CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out var index))
        {
            throw new InvalidOperationException($"Index '{indexName}' does not exist");
        }

        var result = new BulkIndexResult
        {
            IndexName = indexName,
            TotalDocuments = 0,
            SuccessfulDocuments = 0,
            FailedDocuments = 0,
            Errors = new List<string>()
        };

        foreach (var doc in documents)
        {
            result.TotalDocuments++;

            try
            {
                doc.IndexedAt = DateTime.UtcNow;
                index.Documents[doc.Id] = doc;
                result.SuccessfulDocuments++;
            }
            catch (Exception ex)
            {
                result.FailedDocuments++;
                result.Errors.Add($"Document {doc.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to index document {DocumentId}", doc.Id);
            }
        }

        index.TotalDocuments = index.Documents.Count;

        _logger.LogInformation("Bulk indexed {Successful}/{Total} documents in index {IndexName}",
            result.SuccessfulDocuments, result.TotalDocuments, indexName);

        return Task.FromResult(result);
    }

    public Task<bool> DeleteDocumentAsync(
        string indexName,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out var index))
        {
            throw new InvalidOperationException($"Index '{indexName}' does not exist");
        }

        var removed = index.Documents.Remove(documentId);
        if (removed)
        {
            index.TotalDocuments--;
            _logger.LogInformation("Deleted document {DocumentId} from index {IndexName}",
                documentId, indexName);
        }

        return Task.FromResult(removed);
    }

    public Task<SearchResult> SearchAsync(
        string indexName,
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out var index))
        {
            throw new InvalidOperationException($"Index '{indexName}' does not exist");
        }

        options ??= new SearchOptions();
        var normalizedQuery = query.ToLowerInvariant();

        var matchedDocs = index.Documents.Values
            .Select(doc => new
            {
                Document = doc,
                Score = CalculateRelevanceScore(doc, normalizedQuery, options.SearchFields)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Skip(options.Skip)
            .Take(options.Take);

        var results = matchedDocs.Select(x => new SearchResultItem
        {
            DocumentId = x.Document.Id,
            Fields = x.Document.Fields,
            Score = x.Score
        }).ToList();

        _logger.LogInformation("Search for '{Query}' in index {IndexName} returned {Count} results",
            query, indexName, results.Count);

        return Task.FromResult(new SearchResult
        {
            Query = query,
            TotalResults = results.Count,
            Results = results
        });
    }

    public Task<SearchResult> FacetedSearchAsync(
        string indexName,
        string query,
        string[] facets,
        Dictionary<string, string[]>? filters = null,
        CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out var index))
        {
            throw new InvalidOperationException($"Index '{indexName}' does not exist");
        }

        var normalizedQuery = query.ToLowerInvariant();
        var matchedDocs = index.Documents.Values.AsEnumerable();

        // Apply filters
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                var fieldName = filter.Key;
                var allowedValues = filter.Value;

                matchedDocs = matchedDocs.Where(doc =>
                    doc.Fields.TryGetValue(fieldName, out var value) &&
                    allowedValues.Contains(value?.ToString() ?? string.Empty));
            }
        }

        // Calculate relevance and order
        var scoredDocs = matchedDocs
            .Select(doc => new
            {
                Document = doc,
                Score = CalculateRelevanceScore(doc, normalizedQuery, null)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        // Calculate facets
        var facetResults = new Dictionary<string, Dictionary<string, int>>();
        foreach (var facetField in facets)
        {
            var facetCounts = scoredDocs
                .Where(x => x.Document.Fields.ContainsKey(facetField))
                .GroupBy(x => x.Document.Fields[facetField]?.ToString() ?? "")
                .ToDictionary(g => g.Key, g => g.Count());

            facetResults[facetField] = facetCounts;
        }

        var results = scoredDocs.Select(x => new SearchResultItem
        {
            DocumentId = x.Document.Id,
            Fields = x.Document.Fields,
            Score = x.Score
        }).ToList();

        _logger.LogInformation("Faceted search for '{Query}' with {FacetCount} facets returned {Count} results",
            query, facets.Length, results.Count);

        return Task.FromResult(new SearchResult
        {
            Query = query,
            TotalResults = results.Count,
            Results = results,
            Facets = facetResults
        });
    }

    public Task<IEnumerable<string>> AutocompleteAsync(
        string indexName,
        string query,
        string fieldName,
        int maxSuggestions = 10,
        CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out var index))
        {
            throw new InvalidOperationException($"Index '{indexName}' does not exist");
        }

        var normalizedQuery = query.ToLowerInvariant();

        var suggestions = index.Documents.Values
            .Where(doc => doc.Fields.ContainsKey(fieldName))
            .Select(doc => doc.Fields[fieldName]?.ToString() ?? string.Empty)
            .Where(value => value.ToLowerInvariant().Contains(normalizedQuery))
            .Distinct()
            .OrderBy(value => value.Length)
            .Take(maxSuggestions);

        return Task.FromResult<IEnumerable<string>>(suggestions.ToList());
    }

    public Task<bool> CreateIndexAsync(
        string indexName,
        IndexSchema schema,
        CancellationToken cancellationToken = default)
    {
        if (_indexes.ContainsKey(indexName))
        {
            _logger.LogWarning("Index {IndexName} already exists", indexName);
            return Task.FromResult(false);
        }

        _indexes[indexName] = new SearchIndex
        {
            Name = indexName,
            Documents = new Dictionary<string, SearchDocument>(),
            CreatedAt = DateTime.UtcNow,
            TotalDocuments = 0
        };

        _schemas[indexName] = schema;

        _logger.LogInformation("Created index {IndexName} with {FieldCount} fields",
            indexName, schema.Fields.Count);

        return Task.FromResult(true);
    }

    public Task<bool> DeleteIndexAsync(
        string indexName,
        CancellationToken cancellationToken = default)
    {
        var removed = _indexes.Remove(indexName);
        if (removed)
        {
            _schemas.Remove(indexName);
            _logger.LogInformation("Deleted index {IndexName}", indexName);
        }

        return Task.FromResult(removed);
    }

    public Task<IndexStatistics> GetStatisticsAsync(
        string indexName,
        CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out var index))
        {
            throw new InvalidOperationException($"Index '{indexName}' does not exist");
        }

        var stats = new IndexStatistics
        {
            IndexName = indexName,
            TotalDocuments = index.TotalDocuments,
            CreatedAt = index.CreatedAt,
            LastUpdatedAt = index.Documents.Values.Max(d => d.IndexedAt)
        };

        return Task.FromResult(stats);
    }

    private static double CalculateRelevanceScore(
        SearchDocument document,
        string query,
        string[]? searchFields)
    {
        double score = 0;
        var fields = searchFields ?? document.Fields.Keys.ToArray();

        foreach (var fieldName in fields)
        {
            if (!document.Fields.TryGetValue(fieldName, out var value))
                continue;

            var fieldValue = value?.ToString()?.ToLowerInvariant() ?? string.Empty;

            if (fieldValue.Contains(query))
            {
                // Exact match gets higher score
                if (fieldValue == query)
                    score += 10.0;
                // Starts with query gets medium score
                else if (fieldValue.StartsWith(query))
                    score += 5.0;
                // Contains query gets base score
                else
                    score += 2.0;
            }
        }

        return score;
    }
}

/// <summary>
/// Search index.
/// </summary>
public sealed class SearchIndex
{
    public required string Name { get; init; }
    public required Dictionary<string, SearchDocument> Documents { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required int TotalDocuments { get; set; }
}

/// <summary>
/// Search document.
/// </summary>
public sealed class SearchDocument
{
    public required string Id { get; init; }
    public required Dictionary<string, object> Fields { get; init; }
    public required DateTime IndexedAt { get; set; }
}

/// <summary>
/// Index schema.
/// </summary>
public sealed class IndexSchema
{
    public required Dictionary<string, IndexFieldType> Fields { get; init; }
    public string? DefaultSearchField { get; init; }
}

/// <summary>
/// Index field type.
/// </summary>
public enum IndexFieldType
{
    Text,
    Keyword,
    Integer,
    Double,
    Boolean,
    Date
}

/// <summary>
/// Search options.
/// </summary>
public sealed class SearchOptions
{
    public string[]? SearchFields { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 10;
    public Dictionary<string, bool>? Sort { get; init; }
}

/// <summary>
/// Index result.
/// </summary>
public sealed class IndexResult
{
    public required bool Success { get; init; }
    public required string DocumentId { get; init; }
    public required string IndexName { get; init; }
}

/// <summary>
/// Bulk index result.
/// </summary>
public sealed class BulkIndexResult
{
    public required string IndexName { get; init; }
    public required int TotalDocuments { get; init; }
    public required int SuccessfulDocuments { get; init; }
    public required int FailedDocuments { get; init; }
    public required List<string> Errors { get; init; }
}

/// <summary>
/// Search result.
/// </summary>
public sealed class SearchResult
{
    public required string Query { get; init; }
    public required int TotalResults { get; init; }
    public required List<SearchResultItem> Results { get; init; }
    public Dictionary<string, Dictionary<string, int>>? Facets { get; init; }
}

/// <summary>
/// Search result item.
/// </summary>
public sealed class SearchResultItem
{
    public required string DocumentId { get; init; }
    public required Dictionary<string, object> Fields { get; init; }
    public required double Score { get; init; }
}

/// <summary>
/// Index statistics.
/// </summary>
public sealed class IndexStatistics
{
    public required string IndexName { get; init; }
    public required int TotalDocuments { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime? LastUpdatedAt { get; init; }
}
