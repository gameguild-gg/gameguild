using System.Collections.Concurrent;
using System.Reflection;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Locates the repository source tree from the test host and caches a single
///     pass over the product sources. The wiring-gap guards (G2/G4/G5/G6) must see
///     modules that are NOT referenced by the host project — the exact class of gap
///     they exist to catch (see issue #1158: entity configurations existed but the
///     assemblies were never referenced, so nothing reached the compiled model).
///     Reflection over loaded assemblies cannot see such modules; a source scan can.
/// </summary>
public static class WiringGapSourceTree
{
    private static readonly Lazy<string> RepositoryRootLazy = new(FindRepositoryRoot);
    private static readonly Lazy<IReadOnlyList<(string Path, string Text)>> SourceFilesLazy = new(LoadSourceFiles);
    private static readonly Lazy<IReadOnlyList<(string Path, string Text)>> ConfigTextFilesLazy = new(LoadConfigTextFiles);

    public static string RepositoryRoot => RepositoryRootLazy.Value;

    /// <summary>
    ///     "<c>apps/api/Source</c>" — the product source root (host + modules + SharedKernel).
    /// </summary>
    public static string AppsApiSourcePath => Path.Combine(RepositoryRoot, "apps", "api", "Source");

    /// <summary>
    ///     All <c>*.cs</c> files under <c>apps/api/Source</c> with their full text.
    /// </summary>
    public static IReadOnlyList<(string Path, string Text)> SourceFiles => SourceFilesLazy.Value;

    /// <summary>
    ///     Deployment/config surface files (compose files, deploy manifests, Dockerfiles,
    ///     env templates) whose text may carry <c>Section__Path</c> environment bindings.
    /// </summary>
    public static IReadOnlyList<(string Path, string Text)> ConfigTextFiles => ConfigTextFilesLazy.Value;

    /// <summary>
    ///     All <c>appsettings*.json</c> files of the API host.
    /// </summary>
    public static IReadOnlyList<string> AppSettingsFiles =>
        Directory.EnumerateFiles(
                Path.Combine(AppsApiSourcePath, "GameGuild.API"),
                "appsettings*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "apps", "api", "Source", "Modules")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the repository root (a directory containing apps/api/Source/Modules) " +
            $"above '{AppContext.BaseDirectory}'. The wiring-gap guards need the checked-out source tree.");
    }

    private static IReadOnlyList<(string Path, string Text)> LoadSourceFiles()
    {
        var files = new ConcurrentBag<(string Path, string Text)>();
        Parallel.ForEach(
            Directory.EnumerateFiles(AppsApiSourcePath, "*.cs", SearchOption.AllDirectories),
            path =>
            {
                try
                {
                    files.Add((path, File.ReadAllText(path)));
                }
                catch (IOException)
                {
                    // Unreadable file: the guard sees fewer sources, which can only
                    // produce false negatives, never false positives.
                }
            });
        return files.OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
    }

    private static IReadOnlyList<(string Path, string Text)> LoadConfigTextFiles()
    {
        var files = new List<(string Path, string Text)>();
        foreach (var (directory, depthLimit) in new[]
                 {
                     (RepositoryRoot, 1), // root compose files
                     (Path.Combine(RepositoryRoot, "deploy"), int.MaxValue),
                     (Path.Combine(RepositoryRoot, "apps", "api"), int.MaxValue),
                 })
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(
                         directory, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(RepositoryRoot, path);
                if (relative.Split(Path.DirectorySeparatorChar).Length - 1 > depthLimit
                    || Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
                {
                    continue;
                }

                var name = Path.GetFileName(path);
                if (!(name.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                      || name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                      || name.EndsWith(".env", StringComparison.OrdinalIgnoreCase)
                      || name.StartsWith("Dockerfile", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                try
                {
                    files.Add((path, File.ReadAllText(path)));
                }
                catch (IOException)
                {
                }
            }
        }

        return files;
    }

    /// <summary>
    ///     Loads every GameGuild assembly present in the test host output so guards
    ///     can reflect over all modules referenced by the host (same loader approach
    ///     as DurableEventArchitectureTests).
    /// </summary>
    public static IReadOnlyList<Assembly> LoadProductAssemblies()
    {
        foreach (var path in Directory.EnumerateFiles(AppContext.BaseDirectory, "GameGuild*.dll"))
        {
            try
            {
                System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path));
            }
            catch (FileLoadException)
            {
            }
        }

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith("GameGuild", StringComparison.Ordinal) == true)
            .Where(assembly => assembly.GetName().Name?.EndsWith("Tests", StringComparison.Ordinal) != true)
            .ToList();
    }

    /// <summary>
    ///     Enumerates loadable types of an assembly, tolerating unloadable members.
    /// </summary>
    public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
