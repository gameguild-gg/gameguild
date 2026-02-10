using GameGuild.Database;
using GameGuild.Identity.Users;


namespace GameGuild.TestingLab;

/// <summary> Resolvers for TestingRequest GraphQL type </summary>
public class TestingRequestResolvers
{
  public async Task<ProjectVersion?> GetProjectVersion([Parent] TestingRequest request, [Service] ApplicationDbContext context) { return await context.Set<ProjectVersion>().FirstOrDefaultAsync(pv => pv.Id == request.ProjectVersionId).ConfigureAwait(false); }

  public async Task<User?> GetCreatedBy([Parent] TestingRequest request, [Service] ApplicationDbContext context) { return await context.Users.FirstOrDefaultAsync(u => u.Id == request.CreatedById).ConfigureAwait(false); }

  public async Task<IEnumerable<TestingParticipant>> GetParticipants([Parent] TestingRequest request, [Service] ApplicationDbContext context)
  {
    return await context.TestingParticipants.Where(p => p.TestingRequestId == request.Id).ToListAsync().ConfigureAwait(false);
  }

  public static async Task<IEnumerable<TestingSession>> GetSessions([Parent] TestingRequest request, [Service] ApplicationDbContext context)
  {
    return await context.TestingSessions.Where(s => s.TestingRequestId == request.Id).ToListAsync().ConfigureAwait(false);
  }
}
