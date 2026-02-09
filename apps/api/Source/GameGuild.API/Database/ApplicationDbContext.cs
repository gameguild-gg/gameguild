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
        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    ///     Discovers and invokes all <see cref="IModelConfiguration"/> implementations
    ///     from referenced assemblies to build the EF Core model.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Discover all IModelConfiguration implementations from all loaded GameGuild assemblies
        var configurations = AppDomain.CurrentDomain
            .GetAssemblies()
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
}
