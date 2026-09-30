namespace GameGuild.Configuration.PresentationLayer.ProblemDetails;

/// <summary>Controls how API errors are represented as RFC 7807 Problem Details.</summary>
public sealed class ProblemDetailsOptions : BaseOptions
{
    /// <summary>The configuration section name for this options type.</summary>
    public const string SectionName = "ProblemDetails";

    /// <summary>Whether exception text is included in a Detailed response.</summary>
    public bool IncludeExceptionDetails { get; set; }

    /// <summary>Controls the amount of information returned in error responses.</summary>
    public ProblemDetailsDetailLevel DetailLevel { get; set; } = ProblemDetailsDetailLevel.Standard;

    /// <summary>The default RFC 7807 type for otherwise unmapped errors.</summary>
    public string DefaultType { get; set; } = "about:blank";

    /// <summary>The fallback title used when the framework has not supplied one.</summary>
    public string DefaultTitle { get; set; } = "An error occurred.";

    /// <summary>The fallback detail used for standard error responses.</summary>
    public string DefaultDetail { get; set; } = "An unexpected error occurred.";

    /// <summary>Whether to include the request path in the Problem Details instance field.</summary>
    public bool IncludeInstance { get; set; } = true;

    /// <summary>Whether to include the ASP.NET request trace identifier as an extension.</summary>
    public bool IncludeTraceId { get; set; } = true;

    /// <summary>The extension key used for the request trace identifier.</summary>
    public string TraceIdExtensionName { get; set; } = "traceId";

    /// <summary>Whether to echo and include a request correlation identifier.</summary>
    public bool IncludeCorrelationId { get; set; } = true;

    /// <summary>The request and response header used for the correlation identifier.</summary>
    public string CorrelationIdHeaderName { get; set; } = "X-Correlation-ID";

    /// <summary>The extension key used for the correlation identifier.</summary>
    public string CorrelationIdExtensionName { get; set; } = "correlationId";

    /// <summary>
    /// Exception mappings keyed by the exception's fully qualified CLR type name.
    /// The closest matching exception in the inner-exception chain is applied.
    /// </summary>
    public Dictionary<string, ProblemDetailsExceptionMapping> ExceptionMappings { get; set; } =
        CreateDefaultExceptionMappings();

    /// <summary>Static extension values added to every Problem Details response.</summary>
    public Dictionary<string, string> CustomExtensions { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Localized titles and details, keyed first by culture name (for example, "pt-BR")
    /// and then by message key. The built-in keys are "default" and
    /// "database-schema-not-ready"; exception mappings may select another key.
    /// </summary>
    public Dictionary<string, Dictionary<string, ProblemDetailsLocalizedText>> LocalizedMessages { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Fallback status and text for database schema readiness exceptions.</summary>
    public int DatabaseSchemaNotReadyStatusCode { get; set; } = 503;

    public string DatabaseSchemaNotReadyType { get; set; } = "urn:problem-type:database-schema-not-ready";

    public string DatabaseSchemaNotReadyTitle { get; set; } = "Database Schema Not Ready";

    public string DatabaseSchemaNotReadyDetail { get; set; } =
        "Database schema is not ready. Apply pending migrations before retrying.";

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();

        if (!Enum.IsDefined(DetailLevel))
        {
            throw new InvalidOperationException("Problem Details option 'DetailLevel' is not supported.");
        }

        if (ExceptionMappings is null)
        {
            throw new InvalidOperationException("Problem Details option 'ExceptionMappings' cannot be null.");
        }

        if (CustomExtensions is null)
        {
            throw new InvalidOperationException("Problem Details option 'CustomExtensions' cannot be null.");
        }

        if (LocalizedMessages is null)
        {
            throw new InvalidOperationException("Problem Details option 'LocalizedMessages' cannot be null.");
        }

        RequireAbsoluteUri(DefaultType, nameof(DefaultType));
        RequireText(DefaultTitle, nameof(DefaultTitle));
        RequireText(DefaultDetail, nameof(DefaultDetail));
        RequireAbsoluteUri(DatabaseSchemaNotReadyType, nameof(DatabaseSchemaNotReadyType));
        RequireText(DatabaseSchemaNotReadyTitle, nameof(DatabaseSchemaNotReadyTitle));
        RequireText(DatabaseSchemaNotReadyDetail, nameof(DatabaseSchemaNotReadyDetail));
        RequireErrorStatusCode(DatabaseSchemaNotReadyStatusCode, nameof(DatabaseSchemaNotReadyStatusCode));

        if (IncludeExceptionDetails && DetailLevel != ProblemDetailsDetailLevel.Detailed)
        {
            throw new InvalidOperationException(
                "IncludeExceptionDetails requires DetailLevel to be Detailed.");
        }

        if (IncludeTraceId)
        {
            RequireExtensionName(TraceIdExtensionName, nameof(TraceIdExtensionName));
        }

        if (IncludeCorrelationId)
        {
            RequireHeaderName(CorrelationIdHeaderName, nameof(CorrelationIdHeaderName));
            RequireExtensionName(CorrelationIdExtensionName, nameof(CorrelationIdExtensionName));
        }

        if (IncludeTraceId && IncludeCorrelationId &&
            string.Equals(TraceIdExtensionName, CorrelationIdExtensionName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Trace ID and correlation ID extension names must be different.");
        }

        foreach (var (exceptionType, mapping) in ExceptionMappings)
        {
            RequireText(exceptionType, nameof(ExceptionMappings));
            ArgumentNullException.ThrowIfNull(mapping);
            RequireErrorStatusCode(mapping.StatusCode, $"ExceptionMappings[{exceptionType}].StatusCode");
            RequireAbsoluteUri(mapping.Type, $"ExceptionMappings[{exceptionType}].Type");
            RequireText(mapping.Title, $"ExceptionMappings[{exceptionType}].Title");
            if (mapping.Detail is not null && mapping.Detail.Length > 4096)
            {
                throw new InvalidOperationException(
                    $"ExceptionMappings[{exceptionType}].Detail cannot exceed 4096 characters.");
            }

            if (mapping.LocalizedMessageKey is not null)
            {
                RequireText(mapping.LocalizedMessageKey, $"ExceptionMappings[{exceptionType}].LocalizedMessageKey");
            }
        }

        foreach (var (name, _) in CustomExtensions)
        {
            RequireExtensionName(name, nameof(CustomExtensions));
            if (IsReservedExtension(name))
            {
                throw new InvalidOperationException($"Custom extension name '{name}' is reserved.");
            }
        }

        foreach (var (cultureName, messages) in LocalizedMessages)
        {
            if (!IsValidCultureName(cultureName))
            {
                throw new InvalidOperationException(
                    $"LocalizedMessages contains an invalid culture name '{cultureName}'.");
            }

            ArgumentNullException.ThrowIfNull(messages);
            foreach (var (messageKey, localizedText) in messages)
            {
                RequireText(messageKey, $"LocalizedMessages[{cultureName}]");
                ArgumentNullException.ThrowIfNull(localizedText);
                if (localizedText.Title is null && localizedText.Detail is null)
                {
                    throw new InvalidOperationException(
                        $"LocalizedMessages[{cultureName}][{messageKey}] must define a title or detail.");
                }
            }
        }
    }

    /// <summary>Creates default Problem Details options.</summary>
    public static ProblemDetailsOptions CreateDefault() => new();

    /// <summary>
    /// Creates a validated copy so request processing can safely read a fixed configuration snapshot.
    /// </summary>
    public ProblemDetailsOptions CreateSnapshot()
    {
        Validate();
        var snapshot = (ProblemDetailsOptions)MemberwiseClone();
        snapshot.ExceptionMappings = ExceptionMappings.ToDictionary(
            pair => pair.Key,
            pair => new ProblemDetailsExceptionMapping
            {
                StatusCode = pair.Value.StatusCode,
                Type = pair.Value.Type,
                Title = pair.Value.Title,
                Detail = pair.Value.Detail,
                LocalizedMessageKey = pair.Value.LocalizedMessageKey,
            },
            StringComparer.Ordinal);
        snapshot.CustomExtensions = new Dictionary<string, string>(CustomExtensions, StringComparer.Ordinal);
        snapshot.LocalizedMessages = LocalizedMessages.ToDictionary(
            culture => culture.Key,
            culture => culture.Value.ToDictionary(
                message => message.Key,
                message => new ProblemDetailsLocalizedText
                {
                    Title = message.Value.Title,
                    Detail = message.Value.Detail,
                },
                StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
        snapshot.Validate();

        return snapshot;
    }

    private static Dictionary<string, ProblemDetailsExceptionMapping> CreateDefaultExceptionMappings() =>
        new(StringComparer.Ordinal)
        {
            [typeof(ArgumentException).FullName!] = new()
            {
                StatusCode = 400,
                Type = "https://api.gameguild.gg/problems/invalid-argument",
                Title = "Invalid request",
                Detail = "The request contains an invalid argument.",
                LocalizedMessageKey = "invalid-argument",
            },
            [typeof(System.Text.Json.JsonException).FullName!] = new()
            {
                StatusCode = 400,
                Type = "https://api.gameguild.gg/problems/invalid-json",
                Title = "Invalid JSON",
                Detail = "The request body contains invalid JSON.",
                LocalizedMessageKey = "invalid-json",
            },
        };

    private static void RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} cannot be null or empty.");
        }
    }

    private static void RequireAbsoluteUri(string value, string name)
    {
        RequireText(value, name);
        if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            throw new InvalidOperationException($"{name} must be an absolute URI.");
        }
    }

    private static void RequireErrorStatusCode(int statusCode, string name)
    {
        if (statusCode is < 400 or > 599)
        {
            throw new InvalidOperationException($"{name} must be an HTTP error status code between 400 and 599.");
        }
    }

    private static void RequireExtensionName(string value, string name)
    {
        RequireText(value, name);
        if (value.Length > 64 || value.Any(char.IsControl))
        {
            throw new InvalidOperationException($"{name} must be at most 64 printable characters.");
        }
    }

    private static void RequireHeaderName(string value, string name)
    {
        RequireText(value, name);
        const string punctuation = "!#$%&'*+-.^_`|~";
        if (value.Any(character => !char.IsAsciiLetterOrDigit(character) && !punctuation.Contains(character)))
        {
            throw new InvalidOperationException($"{name} must be a valid HTTP header name.");
        }
    }

    private static bool IsValidCultureName(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return false;
        }
        var parts = cultureName.Split('-');
        if (parts[0].Length is < 2 or > 8 || parts[0].Any(character => !char.IsAsciiLetter(character)))
        {
            return false;
        }

        return parts.Skip(1).All(part =>
            part.Length is >= 1 and <= 8 && part.All(char.IsAsciiLetterOrDigit));
    }

    private bool IsReservedExtension(string name) =>
        name is "type" or "title" or "status" or "detail" or "instance" or "errors" or "exception" or "code" ||
        (IncludeTraceId && string.Equals(name, TraceIdExtensionName, StringComparison.Ordinal)) ||
        (IncludeCorrelationId && string.Equals(name, CorrelationIdExtensionName, StringComparison.Ordinal));
}

public enum ProblemDetailsDetailLevel
{
    Minimal,
    Standard,
    Detailed,
}

/// <summary>Maps an exception to a public Problem Details response.</summary>
public sealed class ProblemDetailsExceptionMapping
{
    public int StatusCode { get; set; } = 500;

    public string Type { get; set; } = "about:blank";

    public string Title { get; set; } = "An error occurred.";

    /// <summary>Null uses the configured generic detail; exception text is never used implicitly.</summary>
    public string? Detail { get; set; }

    /// <summary>Optional key into LocalizedMessages for translated title/detail values.</summary>
    public string? LocalizedMessageKey { get; set; }
}

/// <summary>Localized title and detail overrides for a configured problem message.</summary>
public sealed class ProblemDetailsLocalizedText
{
    public string? Title { get; set; }

    public string? Detail { get; set; }
}
