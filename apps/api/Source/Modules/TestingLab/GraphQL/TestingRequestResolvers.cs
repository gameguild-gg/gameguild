using GameGuild.Database;
using GameGuild.Modules.Projects;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.TestingLab;

/// <summary> Resolvers for TestingRequest GraphQL type </summary>
public class TestingRequestResolvers {
  public async Task<ProjectVersion?> GetProjectVersion(
    [Parent] TestingRequest request,
    [Service] ApplicationDbContext context
  ) {
    return await context.Set<ProjectVersion>().FirstOrDefaultAsync(pv => pv.Id == request.ProjectVersionId);
  }

  public async Task<User?> GetCreatedBy(
    [Parent] TestingRequest request,
    [Service] ApplicationDbContext context
  ) {
    return await context.Users.FirstOrDefaultAsync(u => u.Id == request.CreatedById);
  }

  public async Task<IEnumerable<TestingParticipant>> GetParticipants(
    [Parent] TestingRequest request,
    [Service] ApplicationDbContext context
  ) {
    return await context.TestingParticipants.Where(p => p.TestingRequestId == request.Id).ToListAsync();
  }

  public static async Task<IEnumerable<TestingSession>> GetSessions(
    [Parent] TestingRequest request,
    [Service] ApplicationDbContext context
  ) {
    return await context.TestingSessions.Where(s => s.TestingRequestId == request.Id).ToListAsync();
  }
}
