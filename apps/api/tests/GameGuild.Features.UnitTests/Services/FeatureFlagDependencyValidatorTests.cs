using FluentAssertions;
using Moq;


namespace GameGuild.Features.UnitTests.Services;

public class FeatureFlagDependencyValidatorTests
{
    private readonly Mock<IFeatureFlagQueryRepository> _repoMock = new();
    private readonly FeatureFlagDependencyValidator _sut;

    public FeatureFlagDependencyValidatorTests()
    {
        _sut = new FeatureFlagDependencyValidator(_repoMock.Object);
    }

    [Fact]
    public async Task HasCircularDependencyAsync_NoDependencies_ReturnsFalse()
    {
        var flag = new FeatureFlag { Key = "flag-b", Targets = [] };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flag);

        var result = await _sut.HasCircularDependencyAsync("flag-a", "flag-b");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasCircularDependencyAsync_DirectCircle_ReturnsTrue()
    {
        var flagB = new FeatureFlag
        {
            Key = "flag-b",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-a" }]
        };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flagB);

        var result = await _sut.HasCircularDependencyAsync("flag-a", "flag-b");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCircularDependencyAsync_NullFlag_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync((FeatureFlag?)null);

        var result = await _sut.HasCircularDependencyAsync("flag-a", "flag-b");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateDependencyGraphAsync_NoCycle_ReturnsValid()
    {
        var flag = new FeatureFlag { Key = "flag-a", Targets = [] };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-a", default)).ReturnsAsync(flag);

        var (isValid, cycle) = await _sut.ValidateDependencyGraphAsync("flag-a");

        isValid.Should().BeTrue();
        cycle.Should().BeNull();
    }

    [Fact]
    public async Task ValidateDependencyGraphAsync_WithCycle_ReturnsInvalid()
    {
        var flagA = new FeatureFlag
        {
            Key = "flag-a",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-b" }]
        };
        var flagB = new FeatureFlag
        {
            Key = "flag-b",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-a" }]
        };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-a", default)).ReturnsAsync(flagA);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flagB);

        var (isValid, cycle) = await _sut.ValidateDependencyGraphAsync("flag-a");

        isValid.Should().BeFalse();
        cycle.Should().NotBeNull();
        cycle.Should().Contain("flag-a");
    }

    [Fact]
    public async Task GetAllCircularDependenciesAsync_NoFlags_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<FeatureFlag>());

        var result = await _sut.GetAllCircularDependenciesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCircularDependenciesAsync_NoCycles_ReturnsEmpty()
    {
        var flags = new List<FeatureFlag>
        {
            new() { Key = "flag-a", Targets = [] },
            new() { Key = "flag-b", Targets = [] }
        };
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(flags);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-a", default)).ReturnsAsync(flags[0]);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flags[1]);

        var result = await _sut.GetAllCircularDependenciesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCircularDependenciesAsync_WithCycles_FindsCycles()
    {
        var flagA = new FeatureFlag
        {
            Key = "flag-a",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-b" }]
        };
        var flagB = new FeatureFlag
        {
            Key = "flag-b",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-a" }]
        };
        _repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(new List<FeatureFlag> { flagA, flagB });
        _repoMock.Setup(r => r.GetByKeyAsync("flag-a", default)).ReturnsAsync(flagA);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flagB);

        var result = await _sut.GetAllCircularDependenciesAsync();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HasCircularDependencyAsync_IndirectCycle_ReturnsTrue()
    {
        var flagB = new FeatureFlag
        {
            Key = "flag-b",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-c" }]
        };
        var flagC = new FeatureFlag
        {
            Key = "flag-c",
            Targets = [new FeatureFlagTarget { DependsOn = "flag-a" }]
        };
        _repoMock.Setup(r => r.GetByKeyAsync("flag-b", default)).ReturnsAsync(flagB);
        _repoMock.Setup(r => r.GetByKeyAsync("flag-c", default)).ReturnsAsync(flagC);

        var result = await _sut.HasCircularDependencyAsync("flag-a", "flag-b");

        result.Should().BeTrue();
    }
}