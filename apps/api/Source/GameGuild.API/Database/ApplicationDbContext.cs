using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.API.Setup;
using GameGuild.CQRS;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace GameGuild.API.Database;

/// <summary>
///     Thin-shell database context that delegates module-specific configuration
///     to <see cref="IModelConfiguration"/> implementations discovered via assembly scanning.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher? publisher = null)
    : DbContext(options), IApplicationDbContext, IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEventEntities = ChangeTracker.Entries()
            .Select(entry => entry.Entity)
            .OfType<IHasDomainEvents>()
            .Where(entity => entity.DomainEvents.Count > 0)
            .Distinct()
            .ToList();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is EntityBase<Guid> entity &&
                entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(nameof(EntityBase<Guid>.Version)).CurrentValue =
                    (int)entry.Property(nameof(EntityBase<Guid>.Version)).CurrentValue! + 1;
            }
        }

        var affectedRows = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (domainEventEntities.Count == 0)
        {
            return affectedRows;
        }

        var eventPublisher = publisher;
        if (eventPublisher is null)
        {
            try
            {
                eventPublisher = this.GetService<IPublisher>();
            }
            catch (InvalidOperationException)
            {
                return affectedRows;
            }
        }

        var domainEvents = domainEventEntities
            .SelectMany(entity => entity.DomainEvents)
            .ToList();

        foreach (var entity in domainEventEntities)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            await eventPublisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }

        return affectedRows;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var configurations = GetGameGuildAssemblies()
            .SelectMany(LoadTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && typeof(IModelConfiguration).IsAssignableFrom(type)
                           && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(Activator.CreateInstance)
            .OfType<IModelConfiguration>()
            .OrderBy(configuration => configuration.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        foreach (var configuration in configurations)
        {
            configuration.Configure(modelBuilder);
        }

        base.OnModelCreating(modelBuilder);
    }

    private static IEnumerable<Type> LoadTypes(Assembly assembly)
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

    private static IReadOnlyCollection<Assembly> GetGameGuildAssemblies()
    {
        ForceLoadGameGuildAssembliesFromOutput();

        var assembliesByName = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => IsGameGuildAssemblyName(assembly.GetName().Name))
            .GroupBy(assembly => assembly.GetName().Name!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var pending = new Queue<Assembly>(assembliesByName.Values);
        while (pending.Count > 0)
        {
            var assembly = pending.Dequeue();
            foreach (var reference in assembly.GetReferencedAssemblies()
                         .Where(reference => IsGameGuildAssemblyName(reference.Name)))
            {
                TryLoadReferencedAssembly(reference, assembliesByName, pending, Assembly.Load);
            }
        }

        return assembliesByName.Values.ToArray();
    }

    private static bool IsGameGuildAssemblyName(string? name) =>
        name?.StartsWith("GameGuild", StringComparison.Ordinal) == true;

    private static void TryLoadReferencedAssembly(
        AssemblyName reference,
        IDictionary<string, Assembly> assembliesByName,
        Queue<Assembly> pending,
        Func<AssemblyName, Assembly> loadAssembly)
    {
        var referenceName = reference.Name!;
        if (assembliesByName.ContainsKey(referenceName))
            return;

        try
        {
            var loaded = loadAssembly(reference);
            assembliesByName[referenceName] = loaded;
            pending.Enqueue(loaded);
        }
        catch
        {
            // Optional modules may be absent from focused test and design-time hosts.
        }
    }

    private static void ForceLoadGameGuildAssembliesFromOutput()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        foreach (var dll in Directory.GetFiles(baseDirectory, "GameGuild.*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var name = AssemblyName.GetAssemblyName(dll);
                if (ModuleConfiguration.IsTestAssembly(name.Name))
                {
                    continue;
                }

                if (AppDomain.CurrentDomain.GetAssemblies().All(assembly => assembly.FullName != name.FullName))
                {
                    Assembly.LoadFrom(dll);
                }
            }
            catch
            {
                // Optional modules may be absent from focused test and design-time hosts.
            }
        }
    }
}
