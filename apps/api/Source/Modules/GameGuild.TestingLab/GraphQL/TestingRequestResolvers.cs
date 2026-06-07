using GameGuild.Identity.Users;
using GameGuild.Projects;


namespace GameGuild.TestingLab;

/// <summary> Resolvers for TestingRequest GraphQL type </summary>
public class TestingRequestResolvers
{
  public async Task<ProjectVersion?> GetProjectVersion([Parent] TestingRequest request, [Service] IApplicationDbContext context) { return await context.Set<ProjectVersion>().FirstOrDefaultAsync(pv => pv.Id == request.ProjectVersionId).ConfigureAwait(false); }

  public async Task<User?> GetCreatedBy([Parent] TestingRequest request, [Service] IApplicationDbContext context) { return await context.Set<User>().FirstOrDefaultAsync(u => u.Id == request.CreatedById).ConfigureAwait(false); }

  public async Task<IEnumerable<TestingParticipant>> GetParticipants([Parent] TestingRequest request, [Service] IApplicationDbContext context)
  {
    return await context.Set<TestingParticipant>().Where(p => p.TestingRequestId == request.Id).ToListAsync().ConfigureAwait(false);
  }

  public static async Task<IEnumerable<TestingSession>> GetSessions([Parent] TestingRequest request, [Service] IApplicationDbContext context)
  {
    return await context.Set<TestingSession>().Where(s => s.TestingRequestId == request.Id).ToListAsync().ConfigureAwait(false);
  }
}
