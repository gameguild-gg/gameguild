using GameGuild.Modules.TestingLab.Models;
using GameGuild.Modules.TestingLab.Services;

namespace GameGuild.Modules.TestingLab.GraphQL;

/// <summary>
/// GraphQL queries for TestingLab module
/// </summary>
[ExtendObjectType("Query")]
public class TestingLabQueries
{
    /// <summary>
    /// Get all testing requests
    /// </summary>
    public async Task<IEnumerable<TestingRequest>> GetTestingRequests(
        [Service] ITestService testService,
        int skip = 0,
        int take = 50)
    {
        return await testService.GetTestingRequestsAsync(skip, take);
    }

    /// <summary>
    /// Get a specific testing request by ID
    /// </summary>
    public async Task<TestingRequest?> GetTestingRequest(
        [Service] ITestService testService,
        Guid id)
    {
        return await testService.GetTestingRequestByIdAsync(id);
    }

    /// <summary>
    /// Get all testing sessions
    /// </summary>
    public async Task<IEnumerable<TestingSession>> GetTestingSessions(
        [Service] ITestService testService,
        int skip = 0,
        int take = 50)
    {
        return await testService.GetTestingSessionsAsync(skip, take);
    }

    /// <summary>
    /// Get a specific testing session by ID
    /// </summary>
    public async Task<TestingSession?> GetTestingSession(
        [Service] ITestService testService,
        Guid id)
    {
        return await testService.GetTestingSessionByIdAsync(id);
    }
}
