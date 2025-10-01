using GameGuild.Modules.Credentials;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Tests.Credentials.Unit.Validators;

/// <summary>
/// Interface for testing that includes only the DbSets needed for credential validation
/// </summary>
public interface ITestDbContext
{
    DbSet<User> Users { get; }
    DbSet<Credential> Credentials { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}