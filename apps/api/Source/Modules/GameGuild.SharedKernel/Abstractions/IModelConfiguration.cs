using Microsoft.EntityFrameworkCore;

namespace GameGuild;

/// <summary>
///     Interface for module-specific EF Core model configurations.
///     Each module implements this to register its entity configurations
///     without coupling to the central <c>ApplicationDbContext</c>.
/// </summary>
/// <remarks>
///     <para>
///     Implementations are discovered at startup via assembly scanning and invoked
///     during <c>OnModelCreating</c>. This keeps <c>ApplicationDbContext</c> as a thin
///     shell (~40 lines) and lets modules own their own entity mappings.
///     </para>
///     <para>
///     To add a new module's entities to the database, simply implement this interface
///     in the module assembly and ensure the assembly is referenced by the API project.
///     No changes to <c>ApplicationDbContext</c> are required.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     public class MyModelConfiguration : IModelConfiguration
///     {
///         public void Configure(ModelBuilder modelBuilder)
///         {
///             modelBuilder.ApplyConfigurationsFromAssembly(
///                 typeof(MyEntity).Assembly,
///                 type =&gt; type.Namespace?.StartsWith("MyModule") == true);
///         }
///     }
///     </code>
/// </example>
public interface IModelConfiguration
{
    /// <summary>
    ///     Configures the EF Core model for this module's entities.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    void Configure(ModelBuilder modelBuilder);
}
