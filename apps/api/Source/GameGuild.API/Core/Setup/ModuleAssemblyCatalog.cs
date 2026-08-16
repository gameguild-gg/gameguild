using System.Reflection;

namespace GameGuild.API.Setup;

internal static class ModuleAssemblyCatalog
{
    public static Assembly[] Resolve(Assembly entryAssembly, ModuleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(entryAssembly);
        ArgumentNullException.ThrowIfNull(configuration);

        var assemblies = new List<Assembly> { entryAssembly };
        var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            entryAssembly.GetName().Name!
        };

        foreach (var assemblyName in GetRequiredAssemblyNames(configuration))
        {
            if (!loadedNames.Add(assemblyName))
            {
                continue;
            }

            try
            {
                assemblies.Add(Assembly.Load(new AssemblyName(assemblyName)));
            }
            catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                throw new InvalidOperationException(
                    $"Required module assembly '{assemblyName}' could not be loaded.",
                    exception);
            }
        }

        return assemblies.ToArray();
    }

    internal static string[] GetRequiredAssemblyNames(ModuleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.EnabledModules
            .Select(module => $"{configuration.AssemblyPrefix}{module}")
            .Where(name => !configuration.ExcludeTestAssemblies || !ModuleConfiguration.IsTestAssembly(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
