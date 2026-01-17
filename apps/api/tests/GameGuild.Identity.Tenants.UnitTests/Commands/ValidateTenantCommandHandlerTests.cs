using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class ValidateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly ValidateTenantCommandHandler _handler;

    public ValidateTenantCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _handler = new ValidateTenantCommandHandler(_tenantRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Errors_For_Invalid_Name_And_Email()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("", "valid-slug", "invalid"), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "name");
        result.Errors.Should().Contain(e => e.Field == "adminEmail");
    }

    [Fact]
    public async Task Handle_Should_Return_Error_For_Invalid_Slug_Format()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Name", "INVALID SLUG", "admin@example.com"), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "slug");
        result.SlugValidation.Should().NotBeNull();
        result.SlugValidation!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Return_Error_For_Reserved_Slug()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Name", "admin", "admin@example.com"), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "RESERVED");
        result.SlugValidation.Should().NotBeNull();
        result.SlugValidation!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Suggest_Alternatives_When_Slug_Not_Unique()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("team", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("team-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("team-2", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("team-org", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("team-team", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("team-hq", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new ValidateTenantCommand("Name", "team", "admin@example.com"), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ALREADY_EXISTS");
        result.SlugValidation.Should().NotBeNull();
        result.SlugValidation!.SuggestedAlternatives.Should().Contain("team-1");
        result.Suggestions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Return_Valid_For_Correct_Input()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("valid-slug", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Name", "valid-slug", "admin@example.com"), CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
