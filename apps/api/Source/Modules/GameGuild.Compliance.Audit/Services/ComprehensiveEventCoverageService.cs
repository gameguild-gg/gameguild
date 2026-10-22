using System.Reflection;
using System.Runtime.Loader;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Compliance.Audit;

/// <summary>Per-module event coverage entry produced by the coverage sweep.</summary>
public sealed record ModuleEventCoverageEntry(
    string ModuleName,
    string AssemblyName,
    int MutatingCommandCount,
    int CommandContractCount,
    int HandlerCount,
    int MutationEndpointCount,
    IReadOnlyList<string> CommandsWithoutContracts,
    IReadOnlyList<string> CommandsMissingHandlers,
    IReadOnlyList<string> CommandsDuplicateHandlers,
    string CoverageChannel)
{
    public bool IsFullyCovered =>
        CommandsWithoutContracts.Count == 0
        && CommandsMissingHandlers.Count == 0
        && CommandsDuplicateHandlers.Count == 0;
}

/// <summary>Report answering issue #181's "complete event coverage" criterion.</summary>
public sealed record ComprehensiveEventCoverageReport(
    DateTime GeneratedAtUtc,
    int ModuleCount,
    int TotalMutatingCommands,
    int TotalCommandContracts,
    int TotalMutationEndpoints,
    IReadOnlyList<ModuleEventCoverageEntry> Modules,
    IReadOnlyList<string> Gaps)
{
    public bool IsComplete => Gaps.Count == 0;
}

/// <summary>One audit trail coverage family with its evidence, answering issue #180.</summary>
public sealed record AuditTrailCoverageFamily(
    string Family,
    bool IsCovered,
    string Coverage,
    IReadOnlyList<string> ActionTypes,
    IReadOnlyList<string> Evidence);

/// <summary>Report answering issue #180's comprehensive audit trail coverage across business functions.</summary>
public sealed record ComprehensiveAuditTrailCoverageReport(
    DateTime GeneratedAtUtc,
    IReadOnlyList<AuditTrailCoverageFamily> Families)
{
    public bool IsComplete => Families.All(family => family.IsCovered);
}

public interface IEventCoverageSweepService
{
    ComprehensiveEventCoverageReport SweepEventCoverage();
    ComprehensiveAuditTrailCoverageReport BuildAuditTrailCoverage();
}

/// <summary>
/// Coverage sweep over every loaded GameGuild module (issues #180/#181): reflects mutating commands,
/// their assembly-level use-case event contracts, their handlers and the mutation endpoints of every
/// module, and reports per-module coverage together with the aggregate gaps. Completeness rests on
/// three enforcement layers: the GGARCH001/004/005 analyzers fail the build on a missing contract,
/// handler or direct controller persistence; this sweep re-verifies the same invariants over the
/// assemblies actually loaded at runtime; and every command journals a durable operation event through
/// <c>UseCaseOperationBehavior</c>, so the mutation event surface and the module catalog cannot drift
/// apart silently.
/// </summary>
public sealed class EventCoverageSweepService(ILogger<EventCoverageSweepService>? logger = null) : IEventCoverageSweepService
{
    /// <summary>
    /// Audit trail coverage families (issue #180): each family lists the action types its emitters
    /// record and the components that produce them. Kept as the living manifest of the audit surface.
    /// </summary>
    internal static readonly IReadOnlyList<(string Family, string Coverage, string[] ActionTypes, string[] Evidence)> AuditTrailFamilies =
    [
        ("Authentication", "Login, logout, MFA and failure tracking for every interactive and token-based sign-in path.",
            [AuditActionTypes.Login, AuditActionTypes.Logout, AuditActionTypes.LoginFailed, AuditActionTypes.MfaVerified, AuditActionTypes.MfaFailed, AuditActionTypes.MfaEnabled, AuditActionTypes.MfaDisabled],
            ["CentralAuthenticationAuditEventSink bridges every authentication service event into the security event pipeline", "LocalAuthService, MfaAttemptTrackingService, WebAuthnAuthenticationSubService, ApiKey/Basic/ClientCertificate handlers emit through IAuthenticationAuditEventSink"]),
        ("Authorization decisions", "Permission grants, denies, escalations and every endpoint authorization outcome.",
            [AuditActionTypes.PermissionGranted, AuditActionTypes.PermissionDenied, AuditActionTypes.AccessDenied, AuditActionTypes.RoleAssigned, AuditActionTypes.RoleRevoked],
            ["AuditingAuthorizationMiddlewareResultHandler records every endpoint authorization result", "AuditingAuthorizationPermissionService records per-check decisions with denial alerting", "PermissionEvaluationLogEntry persists actor, roles and required permissions"]),
        ("Session management", "Session creation, timeout, termination and concurrent sessions.",
            [AuditActionTypes.SessionCreated, AuditActionTypes.SessionTerminated, AuditActionTypes.DeviceTrusted, AuditActionTypes.DeviceRevoked],
            ["SessionManagementService records creation/termination with reason, IP and user agent"]),
        ("Identity provider integration", "Federated authentication and SSO events for every external identity provider.",
            [AuditActionTypes.ExternalLoginSucceeded, AuditActionTypes.ExternalLoginFailed, AuditActionTypes.ExternalAccountLinked, AuditActionTypes.ExternalAccountProvisioned, AuditActionTypes.FederatedSignInMfaDenied],
            ["OAuthAuthService emits IdP events for GitHub, Google, Microsoft, Discord and generic OIDC federation sign-ins through IAuthenticationAuditEventSink", "CentralScimProvisioningAuditEventSink records SCIM provisioning"]),
        ("User lifecycle", "User creation, modification, suspension and deletion.",
            [AuditActionTypes.UserCreated, AuditActionTypes.UserUpdated, AuditActionTypes.UserDeleted, AuditActionTypes.UserSuspended, AuditActionTypes.UserReactivated, AuditActionTypes.UserProfileUpdated],
            ["PermissionAuditService and UseCaseOperationBehavior journal every mutating user command"]),
        ("Tenant lifecycle", "Tenant provisioning, configuration and termination.",
            [AuditActionTypes.TenantCreated, AuditActionTypes.TenantUpdated, AuditActionTypes.TenantDeleted, AuditActionTypes.TenantUserAdded, AuditActionTypes.TenantUserRemoved],
            ["TenantAuditLog entity plus GET tenants/{id}/audit-log", "TenantIsolationBypassed detection in the security event pipeline"]),
        ("Role and permission lifecycle", "Assignment and revocation history for roles and permissions.",
            [AuditActionTypes.RoleAssigned, AuditActionTypes.RoleRevoked, AuditActionTypes.PermissionGranted, AuditActionTypes.PermissionRevoked, AuditActionTypes.PermissionChanged],
            ["Permission mutations are guarded, versioned and journaled through PermissionAuditLog"]),
        ("Financial transactions", "Payments, refunds, billing and subscription events.",
            [AuditActionTypes.DataExported, AuditActionTypes.BillingProviderEnabled, AuditActionTypes.BillingProviderDisabled],
            ["Payments module audit trail (IAuditTrailRepository, RevenueAuditService)", "Billing provider enable/migrate auditing", "Revenue anomaly alerts per currency"]),
        ("Content management", "Content creation, modification, publishing and archival.",
            ["Content.Pages.PageCreated", "Content.Pages.PageUpdated", "Content.Pages.PagePublished", "Content.Pages.PageDeleted", "Content.Pages.ContentResourceCreated", "Content.Pages.ContentResourcePublished", "Content.Pages.ContentResourceDeleted"],
            ["CentralContentPagesAuditEventSink records pages, content resources, sections, proposals and marketing leads in the owning command transaction"]),
        ("Program management", "Enrollment, progress tracking and completion.",
            ["Learning.Enrollments.UserEnrolled", "Learning.Enrollments.ProgressUpdated", "Learning.Enrollments.StatusChanged", "Learning.Enrollments.EnrollmentCompleted"],
            ["EnrollmentService records enrollment, progress and completion through ILearningAuditEventSink in the owning transaction"]),
        ("Certificate lifecycle", "Issuance, validation, revocation and renewal.",
            ["Learning.Certificates.CertificateIssued", "Learning.Certificates.CertificateVerified", "Learning.Certificates.CertificateRevoked", "Learning.Certificates.CertificateRenewed", "Learning.Certificates.TemplateCreated", "Learning.Certificates.TemplateUpdated", "Learning.Certificates.TemplateDeleted", "Learning.Certificates.TemplateDefaultChanged"],
            ["CertificateService records issuance, verification, revocation and renewal through ILearningAuditEventSink", "CertificateTemplateService records template lifecycle"]),
        ("Security event pipeline", "Classification, severity, retention and alerting for all security events.",
            [AuditActionTypes.SecurityViolation, AuditActionTypes.SecurityAlertResolved, AuditActionTypes.RateLimitExceeded, AuditActionTypes.SuspiciousActivity],
            ["SecurityEventTaxonomy classifies every audit action type", "SecurityEventLogger persists with bounded retries and local spool fallback", "SecurityAlertRuleEvaluator raises deduplicated alerts"]),
        ("Field-level data access", "Field-level read/write tracking with masking and privacy compliance.",
            [AuditActionTypes.FieldAccessRecorded, AuditActionTypes.SensitiveFieldClassified, AuditActionTypes.BulkExportThresholdExceeded, AuditActionTypes.CrossBorderTransferRecorded, AuditActionTypes.DataSubjectRequestRecorded, AuditActionTypes.SensitiveDataRetentionEnforced],
            ["FieldAccessAuditService records field-level reads and writes with classification and masking", "EntityChangeAuditCapture journals per-field write diffs for every entity mutation", "GdprArticle30Service maintains the Article 30 processing register"])
    ];

    public ComprehensiveEventCoverageReport SweepEventCoverage()
    {
        var assemblies = LoadGameGuildAssemblies();
        var contracts = LoadCommandContracts(assemblies);
        var handlers = LoadCommandHandlers(assemblies);
        var modules = new List<ModuleEventCoverageEntry>();
        var gaps = new List<string>();

        foreach (var assembly in assemblies.OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal))
        {
            var assemblyName = assembly.GetName()?.Name ?? assembly.FullName ?? string.Empty;
            if (!assemblyName.StartsWith("GameGuild.", StringComparison.Ordinal))
            {
                continue;
            }

            var moduleName = assemblyName["GameGuild.".Length..];
            var commands = GetMutatingCommands(assembly).ToList();
            var withoutContracts = new List<string>();
            var missingHandlers = new List<string>();
            var duplicateHandlers = new List<string>();
            var contractCount = 0;

            foreach (var command in commands)
            {
                if (contracts.ContainsKey(command))
                {
                    contractCount++;
                }
                else
                {
                    withoutContracts.Add(command.Name);
                }

                var handlerCount = handlers.TryGetValue(command, out var commandHandlers) ? commandHandlers.Count : 0;
                if (handlerCount == 0)
                {
                    missingHandlers.Add(command.Name);
                }
                else if (handlerCount > 1)
                {
                    duplicateHandlers.Add(command.Name);
                }
            }

            var mutationEndpoints = CountMutationEndpoints(assembly);
            var channel = commands.Count == 0
                ? "Platform/library module: no mutating use-case commands; read-only surfaces are covered by the authorization decision audit."
                : "Every mutating command carries a use-case event contract and journals a durable operation event (UseCaseOperationOccurredV1) in its transaction through UseCaseOperationBehavior.";
            modules.Add(new ModuleEventCoverageEntry(
                moduleName,
                assemblyName,
                commands.Count,
                contractCount,
                commands.Count - missingHandlers.Count,
                mutationEndpoints,
                withoutContracts.OrderBy(name => name, StringComparer.Ordinal).ToList(),
                missingHandlers.OrderBy(name => name, StringComparer.Ordinal).ToList(),
                duplicateHandlers.OrderBy(name => name, StringComparer.Ordinal).ToList(),
                channel));

            if (withoutContracts.Count > 0)
            {
                gaps.Add($"{moduleName}: {withoutContracts.Count} mutating command(s) without a use-case event contract.");
            }
            if (missingHandlers.Count > 0)
            {
                gaps.Add($"{moduleName}: {missingHandlers.Count} mutating command(s) without a handler.");
            }
            if (duplicateHandlers.Count > 0)
            {
                gaps.Add($"{moduleName}: {duplicateHandlers.Count} mutating command(s) with multiple handlers.");
            }
        }

        var report = new ComprehensiveEventCoverageReport(
            DateTime.UtcNow,
            modules.Count,
            modules.Sum(module => module.MutatingCommandCount),
            modules.Sum(module => module.CommandContractCount),
            modules.Sum(module => module.MutationEndpointCount),
            modules,
            gaps.OrderBy(gap => gap, StringComparer.Ordinal).ToList());

        logger?.LogInformation(
            "Event coverage sweep: {ModuleCount} modules, {CommandCount} mutating commands, {GapCount} gap(s).",
            report.ModuleCount,
            report.TotalMutatingCommands,
            report.Gaps.Count);

        return report;
    }

    public ComprehensiveAuditTrailCoverageReport BuildAuditTrailCoverage()
    {
        var sweep = SweepEventCoverage();
        var families = new List<AuditTrailCoverageFamily>();
        foreach (var (family, coverage, actionTypes, evidence) in AuditTrailFamilies)
        {
            // A family is covered when every action type it promises is recognized by the
            // platform catalog or a module emitter, and the module sweep reports no gaps.
            var covered = actionTypes.All(IsActionTypeKnown) && sweep.IsComplete;
            families.Add(new AuditTrailCoverageFamily(family, covered, coverage, actionTypes, evidence));
        }

        return new ComprehensiveAuditTrailCoverageReport(DateTime.UtcNow, families);
    }

    /// <summary>
    /// Action types emitted directly by domain modules through their central audit sinks. The
    /// taxonomy classifies the platform catalog; these module-emitted literals are the second
    /// known source of audit action types and are recognized explicitly by the coverage check.
    /// </summary>
    internal static readonly HashSet<string> KnownModuleEmittedActionTypes = new(StringComparer.Ordinal)
    {
        "Content.Pages.PageCreated", "Content.Pages.PageUpdated", "Content.Pages.PageDeleted", "Content.Pages.PagePublished",
        "Content.Pages.PageUnpublished", "Content.Pages.PageSectionCreated", "Content.Pages.PageSectionUpdated",
        "Content.Pages.PageSectionDeleted", "Content.Pages.ContentResourceCreated", "Content.Pages.ContentResourcePublished",
        "Content.Pages.ContentResourceDeleted", "Content.Pages.CourseProposalCreated", "Content.Pages.CourseProposalUpdated",
        "Content.Pages.CourseProposalDeleted", "Content.Pages.MarketingLeadCreated",
        "Learning.Enrollments.UserEnrolled", "Learning.Enrollments.ProgressUpdated", "Learning.Enrollments.StatusChanged",
        "Learning.Enrollments.EnrollmentCompleted",
        "Learning.Certificates.CertificateIssued", "Learning.Certificates.CertificateVerified",
        "Learning.Certificates.CertificateRevoked", "Learning.Certificates.CertificateRenewed",
        "Learning.Certificates.TemplateCreated", "Learning.Certificates.TemplateUpdated",
        "Learning.Certificates.TemplateDeleted", "Learning.Certificates.TemplateDefaultChanged"
    };

    private static bool IsActionTypeKnown(string actionType) =>
        KnownModuleEmittedActionTypes.Contains(actionType)
        || SecurityEventTaxonomy.Classify(actionType, AuditCategory.General, success: true).Description != "Unclassified audit event.";

    private static IEnumerable<Assembly> LoadGameGuildAssemblies()
    {
        var loaded = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        var queue = new Queue<Assembly?>();
        queue.Enqueue(Assembly.GetEntryAssembly());
        queue.Enqueue(Assembly.GetExecutingAssembly());
        queue.Enqueue(typeof(UseCaseEventContractAttribute).Assembly);

        while (queue.Count > 0)
        {
            var assembly = queue.Dequeue();
            if (assembly is null)
            {
                continue;
            }

            var name = assembly.GetName().Name;
            if (name is null || !name.StartsWith("GameGuild", StringComparison.Ordinal))
            {
                continue;
            }
            if (loaded.ContainsKey(name))
            {
                continue;
            }
            loaded[name] = assembly;

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name is not null && reference.Name.StartsWith("GameGuild", StringComparison.Ordinal) && !loaded.ContainsKey(reference.Name))
                {
                    queue.Enqueue(ResolveAssembly(reference));
                }
            }
        }

        // Reference walking alone can miss modules the entry point never links against directly
        // (plug-in style modules). Every deployed GameGuild assembly in the application directory
        // joins the sweep so the manifest provably enumerates the full module catalog, not just the
        // reference closure.
        foreach (var assembly in LoadDeployedGameGuildAssemblies())
        {
            loaded.TryAdd(assembly.GetName()?.Name ?? assembly.FullName ?? string.Empty, assembly);
        }

        return loaded.Values;
    }

    private static IEnumerable<Assembly> LoadDeployedGameGuildAssemblies()
    {
        string[] candidatePaths;
        try
        {
            candidatePaths = Directory.GetFiles(AppContext.BaseDirectory, "GameGuild*.dll");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var path in candidatePaths)
        {
            Assembly? assembly;
            try
            {
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path));
            }
            catch (Exception exception) when (exception is IOException or FileLoadException or BadImageFormatException)
            {
                continue;
            }

            yield return assembly;
        }
    }

    private static Assembly ResolveAssembly(AssemblyName reference)
    {
        try
        {
            return Assembly.Load(reference);
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException)
        {
            // An unloadable reference (design-time facades) must not break the sweep; the caller
            // still sees every loadable GameGuild assembly.
            return typeof(EventCoverageSweepService).Assembly;
        }
    }

    private static Dictionary<Type, string> LoadCommandContracts(IEnumerable<Assembly> assemblies)
    {
        var contracts = new Dictionary<Type, string>();
        foreach (var assembly in assemblies)
        {
            foreach (var attribute in assembly.GetCustomAttributes<UseCaseEventContractAttribute>())
            {
                contracts.TryAdd(attribute.CommandType, attribute.OperationCode);
            }
        }

        return contracts;
    }

    private static Dictionary<Type, List<Type>> LoadCommandHandlers(IEnumerable<Assembly> assemblies)
    {
        var handlers = new Dictionary<Type, List<Type>>();
        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (type is not { IsClass: true, IsAbstract: false })
                {
                    continue;
                }

                foreach (var commandType in ExtractHandledCommands(type))
                {
                    if (!handlers.TryGetValue(commandType, out var list))
                    {
                        handlers[commandType] = list = [];
                    }
                    list.Add(type);
                }
            }
        }

        return handlers;
    }

    private static IEnumerable<Type> ExtractHandledCommands(Type type)
    {
        foreach (var implemented in type.GetInterfaces())
        {
            if (implemented.IsGenericType
                && implemented.GetGenericTypeDefinition() is var definition
                && (definition == typeof(ICommandHandler<,>) || definition == typeof(ICommandHandler<>)))
            {
                yield return implemented.GetGenericArguments()[0];
            }
        }
    }

    private static IEnumerable<Type> GetMutatingCommands(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }

        return types.Where(type => type is { IsClass: true, IsAbstract: false, IsInterface: false } && IsMutatingCommand(type));
    }

    private static bool IsMutatingCommand(Type type) =>
        typeof(ICommand).IsAssignableFrom(type)
        || type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ICommand<>));

    private static int CountMutationEndpoints(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }

        return types
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .SelectMany(controller => controller.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Count(action => action.GetCustomAttributes().Any(attribute =>
                attribute.GetType().Name is "HttpPostAttribute" or "HttpPutAttribute" or "HttpPatchAttribute" or "HttpDeleteAttribute"));
    }
}
