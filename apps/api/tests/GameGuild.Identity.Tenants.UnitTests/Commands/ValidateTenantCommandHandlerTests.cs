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
    public async Task Handle_Should_Return_Error_For_Required_Slug()
    {
        var result = await _handler.Handle(new ValidateTenantCommand("Valid Name", "", "admin@example.com"), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "slug" && e.Code == "REQUIRED");
        result.SlugValidation.Should().NotBeNull();
        result.SlugValidation!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Return_Error_For_Slug_Too_Short()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("ab", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Valid Name", "ab", "admin@example.com"), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "slug" && e.Code == "TOO_SHORT");
    }

    [Fact]
    public async Task Handle_Should_Return_Error_For_Slug_Too_Long()
    {
        var longSlug = new string('a', 51);
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync(longSlug, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Valid Name", longSlug, "admin@example.com"), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "slug" && e.Code == "TOO_LONG");
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
    public async Task Handle_Should_Generate_Random_Slug_Alternatives_When_Suffixes_Are_Unavailable()
    {
        var baseSlug = "team";
        var suffixCandidates = new[] { "team-1", "team-2", "team-org", "team-team", "team-hq" };

        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync(baseSlug, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        foreach (var candidate in suffixCandidates)
        {
            _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync(candidate, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync(
                It.Is<string>(s => s.StartsWith("team-") && s.Length == "team-".Length + 3),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Valid Name", baseSlug, "admin@example.com"), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ALREADY_EXISTS");
        result.SlugValidation!.SuggestedAlternatives.Should().NotBeEmpty();
        result.SlugValidation!.SuggestedAlternatives.Should().OnlyContain(s => s.StartsWith("team-"));
    }

    [Fact]
    public async Task Handle_Should_Return_Errors_For_Name_Too_Short_And_Too_Long()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("valid-slug", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var shortNameResult = await _handler.Handle(new ValidateTenantCommand("A", "valid-slug", "admin@example.com"), CancellationToken.None);
        shortNameResult.IsValid.Should().BeFalse();
        shortNameResult.Errors.Should().Contain(e => e.Field == "name" && e.Code == "TOO_SHORT");

        var longName = new string('a', 101);
        var longNameResult = await _handler.Handle(new ValidateTenantCommand(longName, "valid-slug", "admin@example.com"), CancellationToken.None);
        longNameResult.IsValid.Should().BeFalse();
        longNameResult.Errors.Should().Contain(e => e.Field == "name" && e.Code == "TOO_LONG");
    }

    [Fact]
    public async Task Handle_Should_Warn_For_Name_With_Special_Characters()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("valid-slug", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Acme!", "valid-slug", "admin@example.com"), CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Field == "name" && w.Code == "SPECIAL_CHARACTERS");
    }

    [Fact]
    public async Task Handle_Should_Return_Error_For_AdminEmail_Required()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("valid-slug", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Valid Name", "valid-slug", ""), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "adminEmail" && e.Code == "REQUIRED");
    }

    [Fact]
    public async Task Handle_Should_Return_Error_For_Null_AdminEmail()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("valid-slug", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Valid Name", "valid-slug", null!), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "adminEmail" && e.Code == "REQUIRED");
    }

    [Fact]
    public async Task Handle_Should_Warn_For_Personal_Email_Domain()
    {
        _tenantRepositoryMock.Setup(r => r.IsSlugUniqueAsync("valid-slug", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new ValidateTenantCommand("Valid Name", "valid-slug", "user@gmail.com"), CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Field == "adminEmail" && w.Code == "PERSONAL_EMAIL");
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
