using GameGuild.API.Database;

namespace GameGuild.Projects.UnitTests.Channels;

public sealed class ProjectChannelAvailabilityTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProjectChannelAvailabilityService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ProjectChannelAvailabilityTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new ProjectChannelAvailabilityService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void ProjectChannel_Should_Have_Stable_Explicit_Values()
    {
        ((int)ProjectChannel.Projects).Should().Be(0);
        ((int)ProjectChannel.TestingLab).Should().Be(1);
        ((int)ProjectChannel.LaunchPad).Should().Be(2);
        ((int)ProjectChannel.Store).Should().Be(3);
    }

    [Theory]
    [InlineData(ProjectChannel.Projects)]
    [InlineData(ProjectChannel.TestingLab)]
    [InlineData(ProjectChannel.LaunchPad)]
    [InlineData(ProjectChannel.Store)]
    public async Task GetAsync_Should_Reject_Missing_Projects(ProjectChannel channel)
    {
        var result = await _service.GetAsync(Guid.NewGuid(), channel, _tenantId);

        result.IsAvailable.Should().BeFalse();
        result.Reason.Should().Be(ProjectChannelReasonCodes.ProjectNotFound);
    }

    [Theory]
    [InlineData(ProjectChannel.Projects)]
    [InlineData(ProjectChannel.TestingLab)]
    [InlineData(ProjectChannel.LaunchPad)]
    [InlineData(ProjectChannel.Store)]
    public async Task GetAsync_Should_Reject_SoftDeleted_Projects(ProjectChannel channel)
    {
        var project = AddProject(ContentStatus.Published, ContentVisibility.Public);
        project.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var result = await _service.GetAsync(project.Id, channel, _tenantId);

        result.IsAvailable.Should().BeFalse();
        result.Reason.Should().Be(ProjectChannelReasonCodes.ProjectSoftDeleted);
    }

    [Theory]
    [InlineData(ProjectChannel.Projects)]
    [InlineData(ProjectChannel.TestingLab)]
    [InlineData(ProjectChannel.LaunchPad)]
    [InlineData(ProjectChannel.Store)]
    public async Task GetAsync_Should_Reject_CrossTenant_Projects(ProjectChannel channel)
    {
        var project = AddProject(ContentStatus.Published, ContentVisibility.Public);
        await _context.SaveChangesAsync();

        var result = await _service.GetAsync(project.Id, channel, Guid.NewGuid());

        result.IsAvailable.Should().BeFalse();
        result.Reason.Should().Be(ProjectChannelReasonCodes.TenantMismatch);
    }

    [Theory]
    [InlineData(ProjectChannel.TestingLab, ContentStatus.Archived)]
    [InlineData(ProjectChannel.TestingLab, ContentStatus.Deleted)]
    [InlineData(ProjectChannel.LaunchPad, ContentStatus.Archived)]
    [InlineData(ProjectChannel.LaunchPad, ContentStatus.Deleted)]
    public async Task GetAsync_Should_Reject_Terminal_Lifecycle_For_Internal_Channels(ProjectChannel channel, ContentStatus status)
    {
        var project = AddProject(status, ContentVisibility.Private);
        await _context.SaveChangesAsync();

        var result = await _service.GetAsync(project.Id, channel, _tenantId);

        result.IsAvailable.Should().BeFalse();
        result.Reason.Should().Be(ProjectChannelReasonCodes.LifecycleUnavailable);
    }

    [Theory]
    [InlineData(ProjectChannel.TestingLab)]
    [InlineData(ProjectChannel.LaunchPad)]
    public async Task GetAsync_Should_Allow_Private_Draft_For_Internal_Channels(ProjectChannel channel)
    {
        var project = AddProject(ContentStatus.Draft, ContentVisibility.Private);
        await _context.SaveChangesAsync();

        var result = await _service.GetAsync(project.Id, channel, _tenantId);

        result.IsAvailable.Should().BeTrue();
        result.Reason.Should().Be(ProjectChannelReasonCodes.Available);
    }

    [Fact]
    public async Task GetAsync_Should_Require_Published_Public_For_Store()
    {
        var draft = AddProject(ContentStatus.Draft, ContentVisibility.Public);
        var privateProject = AddProject(ContentStatus.Published, ContentVisibility.Private);
        var available = AddProject(ContentStatus.Published, ContentVisibility.Public);
        await _context.SaveChangesAsync();

        (await _service.GetAsync(draft.Id, ProjectChannel.Store, _tenantId)).Reason.Should().Be(ProjectChannelReasonCodes.NotPublished);
        (await _service.GetAsync(privateProject.Id, ProjectChannel.Store, _tenantId)).Reason.Should().Be(ProjectChannelReasonCodes.NotPublic);
        (await _service.GetAsync(available.Id, ProjectChannel.Store, _tenantId)).IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_Should_Distinguish_Project_Management_From_Public_Projection()
    {
        var project = AddProject(ContentStatus.Draft, ContentVisibility.Private);
        await _context.SaveChangesAsync();

        var management = await _service.GetAsync(project.Id, ProjectChannel.Projects, _tenantId);
        var publicProjection = await _service.GetAsync(project.Id, ProjectChannel.Projects, _tenantId, requirePublicVisibility: true);

        management.IsAvailable.Should().BeTrue();
        publicProjection.IsAvailable.Should().BeFalse();
        publicProjection.Reason.Should().Be(ProjectChannelReasonCodes.NotPublished);
    }

    private Project AddProject(ContentStatus status, ContentVisibility visibility)
    {
        var project = new Project
        {
            Title = Guid.NewGuid().ToString(),
            Slug = Guid.NewGuid().ToString(),
            Status = status,
            Visibility = visibility,
            TenantId = _tenantId
        };
        _context.Set<Project>().Add(project);
        return project;
    }
}
