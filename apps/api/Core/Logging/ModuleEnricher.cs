using Serilog.Core;
using Serilog.Events;

namespace GameGuild.Core.Logging;

/// <summary>
/// Module enricher that adds module information to log events based on the logger category
/// </summary>
public class ModuleEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var moduleName = ExtractModuleName(logEvent);

        if (!string.IsNullOrEmpty(moduleName))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Module", moduleName));
        }

        var componentType = ExtractComponentType(logEvent);

        if (!string.IsNullOrEmpty(componentType))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ComponentType", componentType));
        }
    }

    private static string? ExtractModuleName(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            return null;
        }

        var sourceContextValue = sourceContext.ToString().Trim('"');

        // Extract module name from namespace pattern: GameGuild.Modules.{ModuleName}
        if (sourceContextValue.Contains("GameGuild.Modules.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = sourceContextValue.Split('.');
            var moduleIndex = Array.FindIndex(parts, p => p.Equals("Modules", StringComparison.OrdinalIgnoreCase));

            if (moduleIndex >= 0 && moduleIndex + 1 < parts.Length)
            {
                return parts[moduleIndex + 1];
            }
        }

        // Extract from GameGuild.Core namespace
        if (sourceContextValue.Contains("GameGuild.Core.", StringComparison.OrdinalIgnoreCase))
        {
            return "Core";
        }

        return null;
    }

    private static string? ExtractComponentType(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            return null;
        }

        var sourceContextValue = sourceContext.ToString().Trim('"');

        // Determine component type based on class name patterns
        if (sourceContextValue.Contains("Command", StringComparison.OrdinalIgnoreCase) ||
            sourceContextValue.Contains("Handler", StringComparison.OrdinalIgnoreCase))
        {
            return "Command";
        }

        if (sourceContextValue.Contains("Query", StringComparison.OrdinalIgnoreCase))
        {
            return "Query";
        }

        if (sourceContextValue.Contains("Controller", StringComparison.OrdinalIgnoreCase))
        {
            return "Controller";
        }

        if (sourceContextValue.Contains("Service", StringComparison.OrdinalIgnoreCase))
        {
            return "Service";
        }

        if (sourceContextValue.Contains("Middleware", StringComparison.OrdinalIgnoreCase))
        {
            return "Middleware";
        }

        if (sourceContextValue.Contains("Repository", StringComparison.OrdinalIgnoreCase))
        {
            return "Repository";
        }

        return null;
    }
}
