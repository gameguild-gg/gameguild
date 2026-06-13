using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.API.Database;

/// <summary>
///     Thin-shell database context that delegates all module-specific configuration
///     to <see cref="IModelConfiguration"/> implementations discovered via assembly scanning.
///     Modules own their own entity mappings — adding a new module never requires editing this file.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-increment Version on all tracked EntityBase instances for optimistic concurrency
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is EntityBase<Guid> entity &&
                (entry.State == EntityState.Added || entry.State == EntityState.Modified))
            {
                entry.Property(nameof(EntityBase<Guid>.Version)).CurrentValue =
                    (int)entry.Property(nameof(EntityBase<Guid>.Version)).CurrentValue! + 1;
            }
        }

        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Discovers and invokes all <see cref="IModelConfiguration"/> implementations
    ///     from referenced assemblies to build the EF Core model.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Discover all IModelConfiguration implementations from GameGuild assemblies.
        // Referenced module assemblies are not guaranteed to be loaded before EF builds
        // the model, especially in tests and design-time tooling.
        var configurations = GetGameGuildAssemblies()
            .Where(a => a.FullName?.StartsWith("GameGuild", StringComparison.Ordinal) == true)
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
            })
            .Where(t => t is { IsClass: true, IsAbstract: false }
                         && typeof(IModelConfiguration).IsAssignableFrom(t)
                         && t.GetConstructor(Type.EmptyTypes) is not null)
            .Select(Activator.CreateInstance)
            .OfType<IModelConfiguration>()
            .ToList();

        foreach (var configuration in configurations)
        {
            configuration.Configure(modelBuilder);
        }

        base.OnModelCreating(modelBuilder);
    }

    private static IEnumerable<Assembly> GetGameGuildAssemblies()
    {
        ForceLoadGameGuildAssemblies();

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => a.FullName?.StartsWith("GameGuild", StringComparison.Ordinal) == true);
    }

    private static void ForceLoadGameGuildAssemblies()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        foreach (var dll in Directory.GetFiles(baseDir, "GameGuild.*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var name = AssemblyName.GetAssemblyName(dll);

                if (name.Name?.Contains("Tests", StringComparison.OrdinalIgnoreCase) == true)
                {
                    continue;
                }

                if (AppDomain.CurrentDomain.GetAssemblies().All(a => a.FullName != name.FullName))
                {
                    Assembly.LoadFrom(dll);
                }
            }
            catch
            {
                // Ignore optional module assemblies that cannot be loaded in a given runtime.
            }
        }
    }
}
