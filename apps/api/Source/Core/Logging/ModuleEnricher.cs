using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace GameGuild.Core.Logging;

/// <summary>
/// Module enricher that adds module information to log events based on the logger category
/// </summary>
public class ModuleEnricher : ILogEventEnricher {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
        var moduleName = ExtractModuleName(logEvent);
        if (!string.IsNullOrEmpty(moduleName)) {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Module", moduleName));
        }

        var componentType = ExtractComponentType(logEvent);
        if (!string.IsNullOrEmpty(componentType)) {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ComponentType", componentType));
        }
    }

    private static string? ExtractModuleName(LogEvent logEvent) {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
            return null;

        var sourceContextValue = sourceContext.ToString().Trim('"');

        // Extract module name from namespace patterns like:
        // GameGuild.Modules.Authentication.* -> Authentication
        // GameGuild.Modules.Users.* -> Users
        // GameGuild.Modules.Permissions.* -> Permissions
        if (sourceContextValue.StartsWith("GameGuild.Modules.", StringComparison.OrdinalIgnoreCase)) {
            var parts = sourceContextValue.Split('.');
            if (parts.Length >= 3) {
                return parts[2]; // Extract module name
            }
        }

        // Handle core components
        if (sourceContextValue.StartsWith("GameGuild.Core.", StringComparison.OrdinalIgnoreCase)) {
            return "Core";
        }

        if (sourceContextValue.StartsWith("GameGuild.CQRS.", StringComparison.OrdinalIgnoreCase)) {
            return "CQRS";
        }

        if (sourceContextValue.StartsWith("GameGuild.Database.", StringComparison.OrdinalIgnoreCase)) {
            return "Database";
        }

        return "System";
    }

    private static string? ExtractComponentType(LogEvent logEvent) {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
            return null;

        var sourceContextValue = sourceContext.ToString().Trim('"');

        // Determine component type based on class name patterns
        if (sourceContextValue.Contains("Controller"))
            return "Controller";

        if (sourceContextValue.Contains("Handler"))
            return "Handler";

        if (sourceContextValue.Contains("Service"))
            return "Service";

        if (sourceContextValue.Contains("Middleware"))
            return "Middleware";

        if (sourceContextValue.Contains("Behavior"))
            return "Behavior";

        if (sourceContextValue.Contains("Resolver"))
            return "Resolver";

        if (sourceContextValue.Contains("Repository"))
            return "Repository";

        return null;
    }
}
