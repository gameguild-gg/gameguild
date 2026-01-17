using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class ValidatorTests
{
    [Fact]
    public void CreateTenantCommandValidator_Should_Fail_On_Invalid_Data()
    {
        var validator = new CreateTenantCommandValidator();
        var result = validator.Validate(new CreateTenantCommand("", "INVALID SLUG", "bad-email"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateTenantCommandValidator_Should_Pass_On_Valid_Data()
    {
        var validator = new CreateTenantCommandValidator();
        var result = validator.Validate(new CreateTenantCommand("Name", "valid-slug", "admin@example.com"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddTenantMemberCommandValidator_Should_Fail_On_Empty_Ids()
    {
        var validator = new AddTenantMemberCommandValidator();
        var result = validator.Validate(new TestAddTenantMemberCommand(Guid.Empty, Guid.Empty, ""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemoveTenantMemberCommandValidator_Should_Fail_On_Empty_Ids()
    {
        var validator = new RemoveTenantMemberCommandValidator();
        var result = validator.Validate(new TestRemoveTenantMemberCommand(Guid.Empty, Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TrackUsageCommandValidator_Should_Fail_On_Invalid_Data()
    {
        var validator = new TrackUsageCommandValidator();
        var result = validator.Validate(new TestTrackUsageCommand(Guid.Empty, "", "", 0, -1, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TrackUsageCommandValidator_Should_Pass_On_Valid_Data()
    {
        var validator = new TrackUsageCommandValidator();
        var result = validator.Validate(new TestTrackUsageCommand(Guid.NewGuid(), "api", "call", 1, 0m, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BulkDeactivateTenantsCommandValidator_Should_Fail_On_Empty_List()
    {
        var validator = new BulkDeactivateTenantsCommandValidator();
        var result = validator.Validate(new TestBulkDeactivateTenantsCommand(Array.Empty<Guid>()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BulkDeleteTenantsCommandValidator_Should_Fail_On_Empty_List()
    {
        var validator = new BulkDeleteTenantsCommandValidator();
        var result = validator.Validate(new TestBulkDeleteTenantsCommand(Array.Empty<Guid>(), false));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BulkActivateTenantsCommandValidator_Should_Fail_On_Empty_List()
    {
        var validator = new BulkActivateTenantsCommandValidator();
        var result = validator.Validate(new TestBulkActivateTenantsCommand(Array.Empty<Guid>()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BulkArchiveTenantsCommandValidator_Should_Fail_On_Empty_List()
    {
        var validator = new BulkArchiveTenantsCommandValidator();
        var result = validator.Validate(new TestBulkArchiveTenantsCommand(Array.Empty<Guid>()));

        result.IsValid.Should().BeFalse();
    }

    private sealed record TestAddTenantMemberCommand(Guid TenantId, Guid UserId, string Role, string? InvitedByEmail = null)
        : AddTenantMemberCommand(TenantId, UserId, Role, InvitedByEmail);

    private sealed record TestRemoveTenantMemberCommand(Guid TenantId, Guid UserId)
        : RemoveTenantMemberCommand(TenantId, UserId);

    private sealed record TestTrackUsageCommand(
        Guid TenantId,
        string ResourceType,
        string ActionType,
        int Quantity = 1,
        decimal? Cost = null,
        Dictionary<string, object>? Metadata = null)
        : TrackUsageCommand(TenantId, ResourceType, ActionType, Quantity, Cost, Metadata);

    private sealed record TestBulkDeactivateTenantsCommand(IEnumerable<Guid> TenantIds) : BulkDeactivateTenantsCommand(TenantIds);

    private sealed record TestBulkDeleteTenantsCommand(IEnumerable<Guid> TenantIds, bool HardDelete) : BulkDeleteTenantsCommand(TenantIds, HardDelete);

    private sealed record TestBulkActivateTenantsCommand(IEnumerable<Guid> TenantIds) : BulkActivateTenantsCommand(TenantIds);

    private sealed record TestBulkArchiveTenantsCommand(IEnumerable<Guid> TenantIds) : BulkArchiveTenantsCommand(TenantIds);
}
