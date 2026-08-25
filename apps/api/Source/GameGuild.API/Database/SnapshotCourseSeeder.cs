using System.Text.RegularExpressions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace GameGuild.API.Database;

using CourseProgram = GameGuild.Learning.Courses.Program;

/// <summary>
/// Imports legacy snapshot course content from the main snapshot web data tree into the live API database.
/// This is intentionally a one-shot developer utility, not a normal startup seed.
/// Soft-deleted programs and contents are never re-imported or restored, so administrator
/// deletions survive redeploys even when the import (or force import) runs again.
/// </summary>
public static partial class SnapshotCourseSeeder
{
    private const string ImportSource = "temp/main-snapshot";
    private const string ImportMarkerPrefix = "<!-- gameguild-source:";
    private const string ImportMarkerSuffix = " -->";

    public static async Task<SnapshotCourseImportResult> SeedAsync(
        IServiceProvider serviceProvider,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SnapshotCourseSeeder");
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();

        var coursesRoot = ResolveCoursesRoot(environment.ContentRootPath);
        logger.LogInformation("Importing snapshot courses from {CoursesRoot} (force: {Force})", coursesRoot, force);

        var definitionSet = LoadCourseDefinitions(coursesRoot, logger);
        var definitions = definitionSet.Definitions;

        var importedPrograms = 0;
        var importedContents = 0;

        foreach (var definition in definitions)
        {
            var program = await db.Set<CourseProgram>()
                .IgnoreQueryFilters()
                .Include(item => item.ProgramContents)
                .FirstOrDefaultAsync(item => item.Slug == definition.Slug, cancellationToken)
                .ConfigureAwait(false);

            if (program is not null && program.IsDeleted)
            {
                logger.LogInformation(
                    "Snapshot course program '{Slug}' was soft-deleted. Skipping re-seed to respect the deletion.",
                    definition.Slug);
                continue;
            }

            if (program is not null && !force)
            {
                logger.LogInformation(
                    "Snapshot course program '{Slug}' is already seeded in database. Skipping re-seed (force = false).",
                    definition.Slug);
                continue;
            }

            if (program is null)
            {
                program = new CourseProgram
                {
                    Id = Guid.NewGuid(),
                };

                db.Set<CourseProgram>().Add(program);
                importedPrograms++;
            }

            ApplyProgramDefinition(program, definition);

            var existingContents = await db.Set<ProgramContent>()
                .IgnoreQueryFilters()
                .Where(item => item.ProgramId == program.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var importedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var importedContentLookup = new Dictionary<string, ProgramContent>(StringComparer.OrdinalIgnoreCase);

            foreach (var contentDefinition in definition.Contents.OrderBy(item => item.SortOrder))
            {
                var sourceKey = BuildSourceKey(definition.Slug, contentDefinition.SourceId);
                importedKeys.Add(sourceKey);

                var existingContent = existingContents.FirstOrDefault(item => TryGetImportedSourceKey(item.Body) == sourceKey);

                if (existingContent is not null && existingContent.IsDeleted)
                {
                    logger.LogInformation(
                        "Snapshot content '{SourceKey}' was soft-deleted. Skipping re-seed to respect the deletion.",
                        sourceKey);
                    continue;
                }

                if (existingContent is null)
                {
                    existingContent = new ProgramContent
                    {
                        Id = Guid.NewGuid(),
                        ProgramId = program.Id,
                    };

                    db.Set<ProgramContent>().Add(existingContent);
                    existingContents.Add(existingContent);
                    importedContents++;
                }

                ApplyContentDefinition(existingContent, program.Id, sourceKey, contentDefinition);
                importedContentLookup[sourceKey] = existingContent;
            }

            foreach (var contentDefinition in definition.Contents)
            {
                var sourceKey = BuildSourceKey(definition.Slug, contentDefinition.SourceId);
                if (!importedContentLookup.TryGetValue(sourceKey, out var content))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(contentDefinition.ParentSourceId))
                {
                    content.ParentId = null;
                    continue;
                }

                var parentKey = BuildSourceKey(definition.Slug, contentDefinition.ParentSourceId);
                content.ParentId = importedContentLookup.TryGetValue(parentKey, out var parent)
                    ? parent.Id
                    : null;
            }

            if (definitionSet.IsFallback)
            {
                continue;
            }

            foreach (var staleContent in existingContents)
            {
                var existingSourceKey = TryGetImportedSourceKey(staleContent.Body);
                if (existingSourceKey is null)
                {
                    continue;
                }

                if (existingSourceKey.StartsWith($"{definition.Slug}/", StringComparison.OrdinalIgnoreCase)
                    && !importedKeys.Contains(existingSourceKey)
                    && !staleContent.IsDeleted)
                {
                    staleContent.SoftDelete();
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var publicProgramCount = await db.Set<CourseProgram>()
            .CountAsync(item => item.Status == ContentStatus.Published && item.Visibility == ContentVisibility.Public, cancellationToken)
            .ConfigureAwait(false);
        var databaseName = db.Database.IsRelational()
            ? db.Database.GetDbConnection().Database
            : db.Database.ProviderName ?? "InMemory";

        logger.LogInformation(
            "Imported {ProgramCount} snapshot programs and {ContentCount} snapshot contents from {ImportSource}. DbContext now sees {PublicProgramCount} published/public programs in database {DatabaseName}.",
            definitions.Count,
            definitions.Sum(item => item.Contents.Count),
            definitionSet.IsFallback ? "built-in-snapshot-catalog" : ImportSource,
            publicProgramCount,
            databaseName);

        return new SnapshotCourseImportResult(
            definitions.Count,
            definitions.Sum(item => item.Contents.Count),
            importedPrograms,
            importedContents,
            publicProgramCount,
            databaseName,
            coursesRoot);
    }

    private static void ApplyProgramDefinition(CourseProgram program, SnapshotCourseDefinition definition)
    {
        program.Title = definition.Title;
        program.Description = definition.Description;
        program.Slug = definition.Slug;
        program.Thumbnail = definition.Thumbnail;
        program.EstimatedHours = definition.EstimatedHours;
        program.Category = NormalizeImportedProgramCategory(definition.Slug, definition.Category);
        program.Difficulty = definition.Difficulty;
        program.EnrollmentStatus = definition.EnrollmentStatus;
        program.Status = ContentStatus.Published;
        program.Visibility = ContentVisibility.Public;
        program.SetMetadata("snapshotSource", ImportSource);
        program.SetMetadata("snapshotImportedAt", DateTime.UtcNow);
    }

    private static ProgramCategory NormalizeImportedProgramCategory(string slug, ProgramCategory inferredCategory)
    {
        return NormalizeLabel(slug) switch
        {
            "python" => ProgramCategory.Programming,
            "dsa" => ProgramCategory.Programming,
            "portfolio" => ProgramCategory.Design,
            "intro2gpro" => ProgramCategory.GameDevelopment,
            "networking" => ProgramCategory.GameDevelopment,
            "gamepublishing" => ProgramCategory.Business,
            "databases" => ProgramCategory.Database,
            "dataanalysis" => ProgramCategory.DataScience,
            "ai4games" => ProgramCategory.AI,
            "ai4games2" => ProgramCategory.AI,
            _ => inferredCategory,
        };
    }

    private static void ApplyContentDefinition(ProgramContent content, Guid programId, string sourceKey, SnapshotContentDefinition definition)
    {
        content.ProgramId = programId;
        content.ParentId = null;
        content.Title = definition.Title;
        content.Description = definition.Description;
        content.Type = definition.Type;
        content.LessonFormat = definition.LessonFormat;
        content.SortOrder = definition.SortOrder;
        content.IsRequired = definition.IsRequired;
        content.EstimatedMinutes = definition.EstimatedMinutes;
        content.Visibility = Visibility.Public;
        content.Body = BuildImportedBody(sourceKey, definition.Body);
        content.NormalizeLearningContract();
        content.Touch();
    }

    private static SnapshotCourseDefinition ParseCourseDefinition(string courseDirectory)
    {
        var indexFilePath = Path.Combine(courseDirectory, "index.ts");
        if (!File.Exists(indexFilePath))
        {
            throw new FileNotFoundException($"Course definition file not found: {indexFilePath}");
        }

        var fileText = File.ReadAllText(indexFilePath);
        var markdownImports = ParseMarkdownImports(fileText);
        var programBody = ExtractFirstProgramObject(fileText);

        var slug = ExtractString(programBody, "slug") ?? Path.GetFileName(courseDirectory);
        var title = ExtractString(programBody, "title") ?? slug;
        var description = ExtractString(programBody, "description") ?? string.Empty;
        var thumbnail = ExtractNullableString(programBody, "thumbnail");
        var estimatedHours = ExtractNullableInt(programBody, "estimatedHours");
        var enrollmentStatusComment = ExtractTrailingComment(programBody, "enrollmentStatus");
        var enrollmentStatusRaw = ExtractNullableInt(programBody, "enrollmentStatus");
        var categoryComment = ExtractTrailingComment(programBody, "category");
        var categoryRaw = ExtractNullableInt(programBody, "category");
        var difficultyComment = ExtractTrailingComment(programBody, "difficulty");
        var difficultyRaw = ExtractNullableInt(programBody, "difficulty");

        var contents = ExtractProgramContentObjects(fileText)
            .Select(contentBody => ParseContentDefinition(courseDirectory, markdownImports, contentBody))
            .ToList();

        return new SnapshotCourseDefinition(
            Slug: slug,
            Title: title,
            Description: description,
            Thumbnail: thumbnail,
            EstimatedHours: estimatedHours,
                EnrollmentStatus: ParseEnrollmentStatus(enrollmentStatusComment, enrollmentStatusRaw),
                Category: ParseProgramCategory(categoryComment, categoryRaw, title, description, slug),
                Difficulty: ParseProgramDifficulty(difficultyComment, difficultyRaw, title, description, slug),
            Contents: contents);
    }

    private static SnapshotCourseDefinitionSet LoadCourseDefinitions(string coursesRoot, ILogger logger)
    {
        if (Directory.Exists(coursesRoot))
        {
            var definitions = Directory.GetDirectories(coursesRoot)
                .Select(ParseCourseDefinition)
                .OrderBy(definition => definition.Slug, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (definitions.Count > 0)
            {
                return new SnapshotCourseDefinitionSet(definitions, IsFallback: false);
            }

            logger.LogWarning(
                "Snapshot courses root {CoursesRoot} contains no course directories. Falling back to the built-in course catalog.",
                coursesRoot);
        }
        else
        {
            logger.LogWarning(
                "Snapshot courses root {CoursesRoot} was not found. Falling back to the built-in course catalog.",
                coursesRoot);
        }

        return new SnapshotCourseDefinitionSet(CreateBuiltInCourseDefinitions(), IsFallback: true);
    }

    private static List<SnapshotCourseDefinition> CreateBuiltInCourseDefinitions()
    {
        return
        [
            CreateBuiltInCourse(
                "ai4games",
                "AI for Games",
                "Learn artificial intelligence techniques for game development, including behavioral agents, pathfinding algorithms, procedural content generation, and noise functions.",
                "https://placehold.co/400x225/1f2937/ffffff.png?text=AI+for+Games",
                48,
                ProgramCategory.AI,
                ProgramDifficulty.Intermediate),
            CreateBuiltInCourse(
                "ai4games2",
                "Advanced Game AI",
                "Learn advanced artificial intelligence techniques specifically designed for game development, including pathfinding, decision-making, and procedural content generation.",
                "https://i.imgur.com/cooKXbw.jpeg",
                60,
                ProgramCategory.AI,
                ProgramDifficulty.Advanced),
            CreateBuiltInCourse(
                "dataanalysis",
                "Data Analysis",
                "Learn the fundamentals of data analysis using Python, including data manipulation, visualization, exploratory analysis, and basic statistical methods.",
                "https://placehold.co/400x225/1f2937/ffffff.png?text=Data+Analysis",
                40,
                ProgramCategory.DataScience,
                ProgramDifficulty.Beginner),
            CreateBuiltInCourse(
                "databases",
                "Databases",
                "Design relational databases, write SQL, understand normalization, and compare relational systems with document, key-value, and graph database models.",
                "https://i.imgur.com/D2Sfd70.jpeg",
                48,
                ProgramCategory.Database,
                ProgramDifficulty.Intermediate),
            CreateBuiltInCourse(
                "dsa",
                "Data Structures and Algorithms",
                "Compare core data structures and algorithms for searching, sorting, graph traversal, and performance analysis using practical programming exercises.",
                "https://placehold.co/400x225/1f2937/ffffff.png?text=Data+Structures+%26+Algorithms",
                60,
                ProgramCategory.Programming,
                ProgramDifficulty.Advanced),
            CreateBuiltInCourse(
                "game-publishing",
                "Game Publishing Mastery",
                "Prepare games for release across major platforms with store readiness, submission workflows, release planning, and marketing operations.",
                "https://images.unsplash.com/photo-1556075798-4825dfaaf498?w=400&h=300&fit=crop",
                25,
                ProgramCategory.Business,
                ProgramDifficulty.Advanced),
            CreateBuiltInCourse(
                "intro2gpro",
                "Introduction to Game Programming",
                "Build a practical foundation in game programming roles, workflows, technical vocabulary, and small gameplay systems.",
                "https://placehold.co/400x225/1f2937/ffffff.png?text=Intro+to+Game+Programming",
                45,
                ProgramCategory.GameDevelopment,
                ProgramDifficulty.Beginner),
            CreateBuiltInCourse(
                "networking",
                "Network Programming",
                "Design, implement, and optimize real-time networked applications and games using sockets, serialization, synchronization, and performance tuning.",
                "https://i.imgur.com/Do3392o.jpeg",
                60,
                ProgramCategory.GameDevelopment,
                ProgramDifficulty.Intermediate),
            CreateBuiltInCourse(
                "portfolio",
                "Portfolio Development",
                "Build a professional portfolio that presents projects, process, technical decisions, and outcomes clearly for employers and collaborators.",
                "https://placehold.co/400x225/1f2937/ffffff.png?text=Portfolio+Development",
                30,
                ProgramCategory.Design,
                ProgramDifficulty.Beginner),
            CreateBuiltInCourse(
                "python",
                "Python Programming",
                "Learn computing fundamentals and Python programming through number systems, Boolean logic, algorithm design, and structured implementation.",
                "https://www.python.org/static/community_logos/python-logo-generic.svg",
                40,
                ProgramCategory.Programming,
                ProgramDifficulty.Beginner),
        ];
    }

    private static SnapshotCourseDefinition CreateBuiltInCourse(
        string slug,
        string title,
        string description,
        string thumbnail,
        int estimatedHours,
        ProgramCategory category,
        ProgramDifficulty difficulty)
    {
        return new SnapshotCourseDefinition(
            Slug: slug,
            Title: title,
            Description: description,
            Thumbnail: thumbnail,
            EstimatedHours: estimatedHours,
            EnrollmentStatus: EnrollmentStatus.Open,
            Category: category,
            Difficulty: difficulty,
            Contents:
            [
                new SnapshotContentDefinition(
                    SourceId: "overview",
                    ParentSourceId: null,
                    Title: "Course overview",
                    Description: $"Overview for {title}.",
                    Type: ProgramContentType.Page,
                    LessonFormat: LessonContentFormat.Markdown,
                    SortOrder: 0,
                    IsRequired: true,
                    EstimatedMinutes: 20,
                    Body: description),
            ]);
    }

    private static SnapshotContentDefinition ParseContentDefinition(
        string courseDirectory,
        IReadOnlyDictionary<string, string> markdownImports,
        string contentBody)
    {
        var sourceId = ExtractString(contentBody, "id") ?? Guid.NewGuid().ToString("N");
        var title = ExtractString(contentBody, "title") ?? sourceId;
        var description = ExtractString(contentBody, "description") ?? string.Empty;
        var bodyImportName = ExtractIdentifier(contentBody, "body");
        var body = ResolveContentBody(courseDirectory, markdownImports, contentBody, sourceId, bodyImportName);
        var typeComment = ExtractTrailingComment(contentBody, "type");
        var parentSourceId = ExtractString(contentBody, "parentId");
        var rawType = ExtractNullableInt(contentBody, "type");

        return new SnapshotContentDefinition(
            SourceId: sourceId,
            ParentSourceId: parentSourceId,
            Title: title,
            Description: description,
            Type: ParseProgramContentType(typeComment, rawType, title, description, sourceId, bodyImportName),
            LessonFormat: ParseLessonContentFormat(typeComment, title, description, sourceId, bodyImportName),
            SortOrder: ExtractNullableInt(contentBody, "sortOrder") ?? 0,
            IsRequired: ExtractBoolean(contentBody, "isRequired") ?? true,
            EstimatedMinutes: ExtractNullableInt(contentBody, "estimatedMinutes"),
            Body: body);
    }

    private static string ResolveContentBody(
        string courseDirectory,
        IReadOnlyDictionary<string, string> markdownImports,
        string contentBody,
        string sourceId,
        string? bodyImportName)
    {
        if (!string.IsNullOrWhiteSpace(bodyImportName) && markdownImports.TryGetValue(bodyImportName, out var importPath))
        {
            var markdownPath = ResolveMarkdownPath(courseDirectory, importPath);
            return File.ReadAllText(markdownPath);
        }

        var inlineBody = ExtractString(contentBody, "body");
        if (inlineBody is not null)
        {
            return inlineBody;
        }

        throw new InvalidOperationException($"ProgramContent '{sourceId}' does not declare a markdown import body or inline string body.");
    }

    private static string ResolveCoursesRoot(string contentRootPath)
    {
        var configuredRoot = Environment.GetEnvironmentVariable("SNAPSHOT_COURSES_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot) && Directory.Exists(configuredRoot))
        {
            return configuredRoot;
        }

        const string containerSeedRoot = "/app/seed/courses";
        if (Directory.Exists(containerSeedRoot))
        {
            return containerSeedRoot;
        }

        var repositoryRoot = Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", "..", ".."));
        var snapshotRoot = Path.Combine(repositoryRoot, "temp", "main-snapshot", "apps", "web", "src", "data", "courses");

        if (Directory.Exists(snapshotRoot))
        {
            return snapshotRoot;
        }

        return Path.Combine(repositoryRoot, "apps", "web", "src", "data", "courses");
    }

    private static string ResolveMarkdownPath(string courseDirectory, string relativeMarkdownPath)
    {
        var normalizedRelativePath = relativeMarkdownPath.Replace("./", string.Empty, StringComparison.Ordinal)
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.GetFullPath(Path.Combine(courseDirectory, normalizedRelativePath));
    }

    private static Dictionary<string, string> ParseMarkdownImports(string fileText)
    {
        return MarkdownImportRegex()
            .Matches(fileText)
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => match.Groups["path"].Value,
                StringComparer.Ordinal);
    }

    private static string ExtractFirstProgramObject(string fileText)
    {
        var match = ProgramObjectRegex().Match(fileText);
        if (!match.Success)
        {
            throw new InvalidOperationException("Unable to locate the top-level Program export in the snapshot course definition.");
        }

        return match.Groups["body"].Value;
    }

    private static IEnumerable<string> ExtractProgramContentObjects(string fileText)
    {
        return ProgramContentObjectRegex()
            .Matches(fileText)
            .Select(match => match.Groups["body"].Value);
    }

    private static string? ExtractString(string objectBody, string fieldName)
    {
        var match = Regex.Match(
            objectBody,
            $@"\b{Regex.Escape(fieldName)}\s*:\s*(?<quote>['""])(?<value>(?:\\.|(?!\k<quote>).)*)\k<quote>",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        return match.Success ? Regex.Unescape(match.Groups["value"].Value) : null;
    }

    private static string? ExtractNullableString(string objectBody, string fieldName)
    {
        var nullMatch = Regex.Match(
            objectBody,
            $@"\b{Regex.Escape(fieldName)}\s*:\s*null\b",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        return nullMatch.Success ? null : ExtractString(objectBody, fieldName);
    }

    private static int? ExtractNullableInt(string objectBody, string fieldName)
    {
        var match = Regex.Match(
            objectBody,
            $@"\b{Regex.Escape(fieldName)}\s*:\s*(?<value>-?\d+|null)\b",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        if (!match.Success || string.Equals(match.Groups["value"].Value, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.Parse(match.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool? ExtractBoolean(string objectBody, string fieldName)
    {
        var match = Regex.Match(
            objectBody,
            $@"\b{Regex.Escape(fieldName)}\s*:\s*(?<value>true|false)\b",
            RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        return match.Success ? bool.Parse(match.Groups["value"].Value) : null;
    }

    private static string? ExtractIdentifier(string objectBody, string fieldName)
    {
        var match = Regex.Match(
            objectBody,
            $@"\b{Regex.Escape(fieldName)}\s*:\s*(?<value>[A-Za-z_][A-Za-z0-9_]*)\b",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["value"].Value : null;
    }

    private static string? ExtractTrailingComment(string objectBody, string fieldName)
    {
        var match = Regex.Match(
            objectBody,
            $@"\b{Regex.Escape(fieldName)}\s*:\s*[^\r\n,]+,\s*//\s*(?<comment>[^\r\n]+)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["comment"].Value.Trim() : null;
    }

    private static ProgramCategory ParseProgramCategory(string? categoryComment, int? rawCategory, string title, string description, string slug)
    {
        var normalized = NormalizeLabel(categoryComment);
        var semanticHint = NormalizeLabel($"{categoryComment} {title} {description} {slug}");
        var normalizedSlug = NormalizeLabel(slug);

        if (normalizedSlug.Length > 0)
        {
            return normalizedSlug switch
            {
                "python" => ProgramCategory.Programming,
                "dsa" => ProgramCategory.Programming,
                "portfolio" => ProgramCategory.Design,
                "intro2gpro" => ProgramCategory.GameDevelopment,
                "networking" => ProgramCategory.GameDevelopment,
                "gamepublishing" => ProgramCategory.Business,
                "databases" => ProgramCategory.Database,
                "dataanalysis" => ProgramCategory.DataScience,
                "ai4games" => ProgramCategory.AI,
                "ai4games2" => ProgramCategory.AI,
                _ => ParseProgramCategoryByHeuristic(normalized, semanticHint, rawCategory),
            };
        }

        return ParseProgramCategoryByHeuristic(normalized, semanticHint, rawCategory);
    }

    private static ProgramCategory ParseProgramCategoryByHeuristic(string normalized, string semanticHint, int? rawCategory)
    {
        if (semanticHint.Contains("database", StringComparison.Ordinal)
            || semanticHint.Contains("sql", StringComparison.Ordinal)
            || semanticHint.Contains("nosql", StringComparison.Ordinal))
        {
            return ProgramCategory.Database;
        }

        if (semanticHint.Contains("datascience", StringComparison.Ordinal)
            || semanticHint.Contains("dataanalysis", StringComparison.Ordinal)
            || semanticHint.Contains("analytics", StringComparison.Ordinal)
            || semanticHint.Contains("pandas", StringComparison.Ordinal))
        {
            return ProgramCategory.DataScience;
        }

        if (normalized.Contains("design", StringComparison.Ordinal)
            || semanticHint.Contains("portfolio", StringComparison.Ordinal)
            || semanticHint.Contains("userexperience", StringComparison.Ordinal)
            || semanticHint.Contains("userinterface", StringComparison.Ordinal)
            || semanticHint.Contains("uidesign", StringComparison.Ordinal)
            || semanticHint.Contains("uxdesign", StringComparison.Ordinal)
            || semanticHint.Contains("visualdesign", StringComparison.Ordinal)
            || semanticHint.Contains("interactiondesign", StringComparison.Ordinal))
        {
            return ProgramCategory.Design;
        }

        if (semanticHint.Contains("artificialintelligence", StringComparison.Ordinal)
            || semanticHint.Contains("machinelearning", StringComparison.Ordinal)
            || semanticHint.Contains("pathfinding", StringComparison.Ordinal)
            || semanticHint.Contains("behaviortree", StringComparison.Ordinal)
            || semanticHint.Contains("minmax", StringComparison.Ordinal)
            || semanticHint.Contains("mcts", StringComparison.Ordinal)
            || (semanticHint.Contains("gameai", StringComparison.Ordinal)))
        {
            return ProgramCategory.AI;
        }

        if (semanticHint.Contains("gamedevelopment", StringComparison.Ordinal)
            || semanticHint.Contains("gameprogramming", StringComparison.Ordinal)
            || semanticHint.Contains("unity", StringComparison.Ordinal)
            || semanticHint.Contains("unreal", StringComparison.Ordinal)
            || (semanticHint.Contains("game", StringComparison.Ordinal) && semanticHint.Contains("dev", StringComparison.Ordinal)))
        {
            return ProgramCategory.GameDevelopment;
        }

        if (normalized.Contains("gamedevelopment", StringComparison.Ordinal)
            || (normalized.Contains("game", StringComparison.Ordinal) && normalized.Contains("dev", StringComparison.Ordinal)))
        {
            return ProgramCategory.GameDevelopment;
        }

        if (normalized.Contains("programming", StringComparison.Ordinal))
        {
            return ProgramCategory.Programming;
        }

        if (normalized.Contains("datascience", StringComparison.Ordinal)
            || normalized.Contains("dataanalysis", StringComparison.Ordinal))
        {
            return ProgramCategory.DataScience;
        }

        if (normalized.Contains("database", StringComparison.Ordinal))
        {
            return ProgramCategory.Database;
        }

        if (normalized.Contains("business", StringComparison.Ordinal))
        {
            return ProgramCategory.Business;
        }

        if (normalized.Contains("design", StringComparison.Ordinal))
        {
            return ProgramCategory.Design;
        }

        if (normalized.Contains("webdevelopment", StringComparison.Ordinal)
            || normalized.Contains("web", StringComparison.Ordinal))
        {
            return ProgramCategory.WebDevelopment;
        }

        if (normalized.Contains("ai", StringComparison.Ordinal)
            || normalized.Contains("artificialintelligence", StringComparison.Ordinal)
            || normalized.Contains("machinelearning", StringComparison.Ordinal))
        {
            return ProgramCategory.AI;
        }

        if (rawCategory is not null)
        {
            return rawCategory.Value switch
            {
                0 => ProgramCategory.Programming,
                1 => ProgramCategory.GameDevelopment,
                2 => ProgramCategory.Design,
                3 => ProgramCategory.Business,
                _ => ProgramCategory.Other,
            };
        }

        return ProgramCategory.Other;
    }

    private static ProgramDifficulty ParseProgramDifficulty(string? difficultyComment, int? rawDifficulty, string title, string description, string slug)
    {
        var normalized = NormalizeLabel(difficultyComment);
        var semanticHint = NormalizeLabel($"{difficultyComment} {title} {description} {slug}");

        if (semanticHint.Contains("advanced", StringComparison.Ordinal)
            || semanticHint.Contains("mastery", StringComparison.Ordinal))
        {
            return ProgramDifficulty.Advanced;
        }

        if (semanticHint.Contains("intermediate", StringComparison.Ordinal))
        {
            return ProgramDifficulty.Intermediate;
        }

        if (normalized.Contains("intermediate", StringComparison.Ordinal))
        {
            return ProgramDifficulty.Intermediate;
        }

        if (normalized.Contains("advanced", StringComparison.Ordinal))
        {
            return ProgramDifficulty.Advanced;
        }

        if (normalized.Contains("expert", StringComparison.Ordinal))
        {
            return ProgramDifficulty.Expert;
        }

        if (rawDifficulty is not null)
        {
            return rawDifficulty.Value switch
            {
                1 => ProgramDifficulty.Intermediate,
                2 => ProgramDifficulty.Advanced,
                3 => ProgramDifficulty.Expert,
                _ => ProgramDifficulty.Beginner,
            };
        }

        return ProgramDifficulty.Beginner;
    }

    private static EnrollmentStatus ParseEnrollmentStatus(string? enrollmentStatusComment, int? rawEnrollmentStatus)
    {
        var normalized = NormalizeLabel(enrollmentStatusComment);

        if (normalized.Contains("closed", StringComparison.Ordinal))
        {
            return EnrollmentStatus.Closed;
        }

        if (normalized.Contains("inviteonly", StringComparison.Ordinal))
        {
            return EnrollmentStatus.InviteOnly;
        }

        if (normalized.Contains("waitlist", StringComparison.Ordinal))
        {
            return EnrollmentStatus.Waitlist;
        }

        if (rawEnrollmentStatus is not null)
        {
            return rawEnrollmentStatus.Value switch
            {
                1 => EnrollmentStatus.Closed,
                2 => EnrollmentStatus.InviteOnly,
                3 => EnrollmentStatus.Waitlist,
                _ => EnrollmentStatus.Open,
            };
        }

        return EnrollmentStatus.Open;
    }

    private static ProgramContentType ParseProgramContentType(
        string? typeComment,
        int? rawType,
        string title,
        string description,
        string sourceId,
        string? bodyImportName)
    {
        var normalized = NormalizeLabel(typeComment);
        var semanticHint = NormalizeLabel($"{title} {description} {sourceId} {bodyImportName}");

        if (normalized.Contains("assignment", StringComparison.Ordinal))
        {
            return ProgramContentType.Assignment;
        }

        if (normalized.Contains("questionnaire", StringComparison.Ordinal)
            || normalized.Contains("quiz", StringComparison.Ordinal)
            || normalized.Contains("test", StringComparison.Ordinal))
        {
            return ProgramContentType.Questionnaire;
        }

        if (normalized.Contains("discussion", StringComparison.Ordinal))
        {
            return ProgramContentType.Discussion;
        }

        if (normalized.Contains("code", StringComparison.Ordinal))
        {
            return ProgramContentType.Code;
        }

        if (normalized.Contains("challenge", StringComparison.Ordinal))
        {
            return ProgramContentType.Challenge;
        }

        if (normalized.Contains("reflection", StringComparison.Ordinal))
        {
            return ProgramContentType.Reflection;
        }

        if (normalized.Contains("survey", StringComparison.Ordinal))
        {
            return ProgramContentType.Survey;
        }

        if (normalized.Contains("lesson", StringComparison.Ordinal))
        {
            return ProgramContentType.Lesson;
        }

        if (semanticHint.Contains("syllabus", StringComparison.Ordinal)
            || semanticHint.Contains("lecture", StringComparison.Ordinal)
            || semanticHint.Contains("readings", StringComparison.Ordinal)
            || semanticHint.Contains("reveal", StringComparison.Ordinal)
            || semanticHint.Contains("slides", StringComparison.Ordinal))
        {
            return ProgramContentType.Lesson;
        }

        if (normalized.Contains("page", StringComparison.Ordinal))
        {
            return ProgramContentType.Page;
        }

        if (semanticHint.Contains("quiz", StringComparison.Ordinal))
        {
            return ProgramContentType.Questionnaire;
        }

        if (semanticHint.Contains("assignment", StringComparison.Ordinal)
            || semanticHint.Contains("exercise", StringComparison.Ordinal)
            || semanticHint.Contains("project", StringComparison.Ordinal)
            || semanticHint.Contains("midterm", StringComparison.Ordinal)
            || semanticHint.Contains("final", StringComparison.Ordinal))
        {
            return ProgramContentType.Assignment;
        }

        return rawType switch
        {
            0 => ProgramContentType.Page,
            1 => ProgramContentType.Lesson,
            2 => ProgramContentType.Assignment,
            3 => ProgramContentType.Questionnaire,
            _ => ProgramContentType.Page,
        };
    }

    private static string BuildSourceKey(string programSlug, string sourceId)
    {
        return $"{programSlug}/{sourceId}";
    }

    private static LessonContentFormat ParseLessonContentFormat(
        string? typeComment,
        string title,
        string description,
        string sourceId,
        string? bodyImportName)
    {
        var semanticHint = NormalizeLabel(string.Join(
            ' ',
            new[] { typeComment, title, description, sourceId, bodyImportName }.Where(value => !string.IsNullOrWhiteSpace(value))));

        if (semanticHint.Contains("reveal", StringComparison.Ordinal) ||
            semanticHint.Contains("slides", StringComparison.Ordinal))
        {
            return LessonContentFormat.RevealJs;
        }

        if (semanticHint.Contains("video", StringComparison.Ordinal))
        {
            return LessonContentFormat.Video;
        }

        return LessonContentFormat.Markdown;
    }

    private static string BuildImportedBody(string sourceKey, string markdownBody)
    {
        var trimmedBody = markdownBody.Trim();
        return $"{ImportMarkerPrefix}{sourceKey}{ImportMarkerSuffix}{Environment.NewLine}{Environment.NewLine}{trimmedBody}";
    }

    private static string? TryGetImportedSourceKey(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var match = Regex.Match(body, @"<!--\s*gameguild-source:(?<key>[^>]+)\s*-->", RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["key"].Value.Trim() : null;
    }

    private static string NormalizeLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value, "[^A-Za-z0-9]+", string.Empty).ToLowerInvariant();
    }

    [GeneratedRegex(@"import\s+(?<name>[A-Za-z0-9_]+)\s+from\s+['""](?<path>\.[^'""]+\.md)['""];", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownImportRegex();

    [GeneratedRegex(@"export const\s+\w+Program:\s*Program\s*=\s*\{(?<body>.*?)^\};", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex ProgramObjectRegex();

    [GeneratedRegex(@"export const\s+\w+Content:\s*ProgramContent\s*=\s*\{(?<body>.*?)^\};", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex ProgramContentObjectRegex();
}

public sealed record SnapshotCourseImportResult(
    int ParsedPrograms,
    int ParsedContents,
    int CreatedPrograms,
    int CreatedContents,
    int PublicProgramCount,
    string DatabaseName,
    string CoursesRoot);

internal sealed record SnapshotCourseDefinition(
    string Slug,
    string Title,
    string Description,
    string? Thumbnail,
    int? EstimatedHours,
    EnrollmentStatus EnrollmentStatus,
    ProgramCategory Category,
    ProgramDifficulty Difficulty,
    IReadOnlyList<SnapshotContentDefinition> Contents);

internal sealed record SnapshotCourseDefinitionSet(
    IReadOnlyList<SnapshotCourseDefinition> Definitions,
    bool IsFallback);

internal sealed record SnapshotContentDefinition(
    string SourceId,
    string? ParentSourceId,
    string Title,
    string Description,
    ProgramContentType Type,
    LessonContentFormat LessonFormat,
    int SortOrder,
    bool IsRequired,
    int? EstimatedMinutes,
    string Body);
