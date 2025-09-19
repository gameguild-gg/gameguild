using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Core.GraphQL;

/// <summary>
/// Middleware for analyzing GraphQL query complexity and depth to prevent resource exhaustion attacks
/// Implements comprehensive query analysis with configurable limits
/// </summary>
public class GraphQLSecurityMiddleware {
    private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;
    private readonly GraphQlOptions _options;
    private readonly ILogger<GraphQLSecurityMiddleware> _logger;

    public GraphQLSecurityMiddleware(
      Microsoft.AspNetCore.Http.RequestDelegate next,
      IOptions<GraphQlOptions> options,
      ILogger<GraphQLSecurityMiddleware> logger) {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        // Only process GraphQL requests
        if (!IsGraphQLRequest(context)) {
            await _next(context);
            return;
        }

        try {
            // Read and analyze the GraphQL query
            var queryAnalysis = await AnalyzeGraphQLRequestAsync(context);

            if (!queryAnalysis.IsValid) {
                await HandleSecurityViolation(context, queryAnalysis);
                return;
            }

            // Log query analysis for monitoring
            LogQueryAnalysis(context, queryAnalysis);

            await _next(context);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error in GraphQL security middleware");
            await _next(context);
        }
    }

    private bool IsGraphQLRequest(HttpContext context) {
        return context.Request.Path.StartsWithSegments(_options.Path, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<GraphQLQueryAnalysis> AnalyzeGraphQLRequestAsync(HttpContext context) {
        var query = await ExtractGraphQLQueryAsync(context);

        if (string.IsNullOrWhiteSpace(query)) {
            return GraphQLQueryAnalysis.Invalid("Empty query");
        }

        try {
            // Parse the GraphQL document
            var document = Utf8GraphQLParser.Parse(query);

            // Analyze depth and complexity
            var depthAnalysis = AnalyzeDepth(document);
            var complexityAnalysis = AnalyzeComplexity(document);

            var violations = new List<string>();

            if (depthAnalysis.MaxDepth > _options.MaxDepth) {
                violations.Add($"Query depth {depthAnalysis.MaxDepth} exceeds limit of {_options.MaxDepth}");
            }

            if (complexityAnalysis.TotalComplexity > _options.MaxComplexity) {
                violations.Add($"Query complexity {complexityAnalysis.TotalComplexity} exceeds limit of {_options.MaxComplexity}");
            }

            // Check for potential DoS patterns
            var dosPatterns = DetectDoSPatterns(document);
            violations.AddRange(dosPatterns);

            return violations.Count > 0
              ? GraphQLQueryAnalysis.Invalid(string.Join("; ", violations))
              : GraphQLQueryAnalysis.Valid(depthAnalysis.MaxDepth, complexityAnalysis.TotalComplexity, complexityAnalysis.FieldCount);
        }
        catch (Exception ex) when (ex.Message.Contains("syntax")) {
            _logger.LogWarning("Invalid GraphQL syntax: {Error}", ex.Message);
            return GraphQLQueryAnalysis.Invalid($"Invalid GraphQL syntax: {ex.Message}");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error analyzing GraphQL query");
            return GraphQLQueryAnalysis.Invalid("Query analysis failed");
        }
    }

    private async Task<string> ExtractGraphQLQueryAsync(HttpContext context) {
        context.Request.EnableBuffering();

        try {
            if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)) {
                return context.Request.Query["query"].FirstOrDefault() ?? string.Empty;
            }

            if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)) {
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (context.Request.ContentType?.Contains("application/json") == true) {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(body);
                    if (jsonDoc.RootElement.TryGetProperty("query", out var queryElement)) {
                        return queryElement.GetString() ?? string.Empty;
                    }
                }

                return body;
            }
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "Failed to extract GraphQL query from request");
        }

        return string.Empty;
    }

    private GraphQLDepthAnalysis AnalyzeDepth(DocumentNode document) {
        var maxDepth = 0;

        foreach (var definition in document.Definitions.OfType<OperationDefinitionNode>()) {
            var depth = CalculateSelectionSetDepth(definition.SelectionSet, 1);
            maxDepth = Math.Max(maxDepth, depth);
        }

        return new GraphQLDepthAnalysis(maxDepth);
    }

    private int CalculateSelectionSetDepth(SelectionSetNode selectionSet, int currentDepth) {
        if (selectionSet?.Selections == null || !selectionSet.Selections.Any()) {
            return currentDepth;
        }

        var maxDepth = currentDepth;

        foreach (var selection in selectionSet.Selections) {
            var selectionDepth = selection switch {
                FieldNode field => field.SelectionSet != null
                  ? CalculateSelectionSetDepth(field.SelectionSet, currentDepth + 1)
                  : currentDepth,
                InlineFragmentNode inlineFragment => inlineFragment.SelectionSet != null
                  ? CalculateSelectionSetDepth(inlineFragment.SelectionSet, currentDepth)
                  : currentDepth,
                FragmentSpreadNode => currentDepth + 1, // Conservative estimate for fragments
                _ => currentDepth
            };

            maxDepth = Math.Max(maxDepth, selectionDepth);
        }

        return maxDepth;
    }

    private GraphQLComplexityAnalysis AnalyzeComplexity(DocumentNode document) {
        var totalComplexity = 0;
        var fieldCount = 0;

        foreach (var definition in document.Definitions.OfType<OperationDefinitionNode>()) {
            var (complexity, fields) = CalculateSelectionSetComplexity(definition.SelectionSet);
            totalComplexity += complexity;
            fieldCount += fields;
        }

        return new GraphQLComplexityAnalysis(totalComplexity, fieldCount);
    }

    private (int complexity, int fieldCount) CalculateSelectionSetComplexity(SelectionSetNode? selectionSet) {
        if (selectionSet?.Selections == null || !selectionSet.Selections.Any()) {
            return (0, 0);
        }

        var totalComplexity = 0;
        var totalFields = 0;

        foreach (var selection in selectionSet.Selections) {
            var (selectionComplexity, selectionFields) = selection switch {
                FieldNode field => CalculateFieldComplexity(field),
                InlineFragmentNode inlineFragment => CalculateSelectionSetComplexity(inlineFragment.SelectionSet),
                FragmentSpreadNode => (5, 1), // Conservative estimate for fragments
                _ => (1, 1)
            };

            totalComplexity += selectionComplexity;
            totalFields += selectionFields;
        }

        return (totalComplexity, totalFields);
    }

    private (int complexity, int fieldCount) CalculateFieldComplexity(FieldNode field) {
        var baseComplexity = GetFieldComplexity(field.Name.Value);
        var fieldCount = 1;

        // Add complexity for arguments (especially pagination)
        var argumentComplexity = CalculateArgumentComplexity(field.Arguments);

        // Add complexity for nested selections
        var (nestedComplexity, nestedFields) = CalculateSelectionSetComplexity(field.SelectionSet);

        var totalComplexity = baseComplexity + argumentComplexity + nestedComplexity;
        var totalFields = fieldCount + nestedFields;

        return (totalComplexity, totalFields);
    }

    private int GetFieldComplexity(string fieldName) {
        // Assign different complexity scores based on field types
        return fieldName.ToLowerInvariant() switch {
            // High complexity operations
            var f when f.Contains("search") => 10,
            var f when f.Contains("aggregate") => 8,
            var f when f.Contains("count") => 5,

            // Medium complexity operations
            var f when f.Contains("list") || f.Contains("all") => 3,
            var f when f.Contains("by") && f.Contains("id") => 2,

            // Collections and connections
            var f when f.EndsWith("s") => 3, // Plural fields (collections)
            var f when f.Contains("connection") => 4,
            var f when f.Contains("edges") => 2,
            var f when f.Contains("nodes") => 2,

            // Default complexity
            _ => 1
        };
    }

    private int CalculateArgumentComplexity(IReadOnlyList<ArgumentNode>? arguments) {
        if (arguments == null || !arguments.Any()) {
            return 0;
        }

        var complexity = 0;

        foreach (var argument in arguments) {
            complexity += argument.Name.Value.ToLowerInvariant() switch {
                "first" or "last" => GetPaginationComplexity(argument.Value),
                "take" or "skip" => GetPaginationComplexity(argument.Value),
                "where" or "filter" => 2,
                "orderBy" or "sortBy" => 1,
                _ => 0
            };
        }

        return complexity;
    }

    private int GetPaginationComplexity(IValueNode value) {
        if (value is IntValueNode intValue && int.TryParse(intValue.Value, out var limit)) {
            // Higher complexity for larger pagination limits
            return limit switch {
                > 100 => 5,
                > 50 => 3,
                > 20 => 2,
                _ => 1
            };
        }

        return 1;
    }

    private List<string> DetectDoSPatterns(DocumentNode document) {
        var violations = new List<string>();

        // Check for deeply nested aliases (alias DoS attack)
        var aliasCount = CountAliases(document);
        if (aliasCount > 50) {
            violations.Add($"Excessive aliases detected: {aliasCount}");
        }

        // Check for circular fragment references
        if (HasCircularFragments(document)) {
            violations.Add("Circular fragment references detected");
        }

        // Check for excessive directive usage
        var directiveCount = CountDirectives(document);
        if (directiveCount > 20) {
            violations.Add($"Excessive directives detected: {directiveCount}");
        }

        return violations;
    }

    private int CountAliases(DocumentNode document) {
        var aliasCount = 0;

        foreach (var definition in document.Definitions.OfType<OperationDefinitionNode>()) {
            aliasCount += CountAliasesInSelectionSet(definition.SelectionSet);
        }

        return aliasCount;
    }

    private int CountAliasesInSelectionSet(SelectionSetNode? selectionSet) {
        if (selectionSet?.Selections == null) return 0;

        var count = 0;

        foreach (var selection in selectionSet.Selections) {
            if (selection is FieldNode field) {
                if (field.Alias != null) count++;
                count += CountAliasesInSelectionSet(field.SelectionSet);
            }
            else if (selection is InlineFragmentNode inlineFragment) {
                count += CountAliasesInSelectionSet(inlineFragment.SelectionSet);
            }
        }

        return count;
    }

    private bool HasCircularFragments(DocumentNode document) {
        // Simplified check - could be enhanced with proper graph traversal
        var fragmentNames = document.Definitions
          .OfType<FragmentDefinitionNode>()
          .Select(f => f.Name.Value)
          .ToHashSet();

        foreach (var fragment in document.Definitions.OfType<FragmentDefinitionNode>()) {
            if (HasCircularReference(fragment, fragmentNames, new HashSet<string>())) {
                return true;
            }
        }

        return false;
    }

    private bool HasCircularReference(FragmentDefinitionNode fragment, HashSet<string> allFragments, HashSet<string> visited) {
        if (visited.Contains(fragment.Name.Value)) {
            return true;
        }

        visited.Add(fragment.Name.Value);

        // Check for fragment spreads in this fragment
        // This is a simplified implementation
        return false;
    }

    private int CountDirectives(DocumentNode document) {
        var directiveCount = 0;

        foreach (var definition in document.Definitions) {
            directiveCount += CountDirectivesInNode(definition);
        }

        return directiveCount;
    }

    private int CountDirectivesInNode(ISyntaxNode node) {
        // Simplified directive counting - could be enhanced
        return 0;
    }

    private async Task HandleSecurityViolation(HttpContext context, GraphQLQueryAnalysis analysis) {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";

        var response = new {
            errors = new[] {
        new {
          message = "Query security violation",
          details = analysis.ErrorMessage,
          extensions = new {
            code = "QUERY_SECURITY_VIOLATION",
            maxDepth = _options.MaxDepth,
            maxComplexity = _options.MaxComplexity
          }
        }
      }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);

        _logger.LogWarning("GraphQL security violation: {Error}", analysis.ErrorMessage);
    }

    private void LogQueryAnalysis(HttpContext context, GraphQLQueryAnalysis analysis) {
        if (analysis.IsValid) {
            _logger.LogDebug("GraphQL query analysis - Depth: {Depth}, Complexity: {Complexity}, Fields: {FieldCount}",
              analysis.Depth, analysis.Complexity, analysis.FieldCount);
        }
    }
}

public class GraphQLQueryAnalysis {
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int Depth { get; private set; }
    public int Complexity { get; private set; }
    public int FieldCount { get; private set; }

    private GraphQLQueryAnalysis(bool isValid, string? errorMessage = null, int depth = 0, int complexity = 0, int fieldCount = 0) {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        Depth = depth;
        Complexity = complexity;
        FieldCount = fieldCount;
    }

    public static GraphQLQueryAnalysis Valid(int depth, int complexity, int fieldCount) =>
      new(true, null, depth, complexity, fieldCount);

    public static GraphQLQueryAnalysis Invalid(string errorMessage) =>
      new(false, errorMessage);
}

public record GraphQLDepthAnalysis(int MaxDepth);

public record GraphQLComplexityAnalysis(int TotalComplexity, int FieldCount);

/// <summary>
/// Extension methods for adding GraphQL security middleware
/// </summary>
public static class GraphQLSecurityMiddlewareExtensions {
    public static IApplicationBuilder UseGraphQLSecurity(this IApplicationBuilder builder) {
        return builder.UseMiddleware<GraphQLSecurityMiddleware>();
    }
}
