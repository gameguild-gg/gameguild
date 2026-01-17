using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class QueryValidatorTests
{
    [Fact]
    public void GetTenantByIdQueryValidator_Should_Fail_On_Empty_TenantId()
    {
        var validator = new GetTenantByIdQueryValidator();
        var result = validator.Validate(new GetTenantByIdQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void GetTenantByIdQueryValidator_Should_Pass_On_Valid_TenantId()
    {
        var validator = new GetTenantByIdQueryValidator();
        var result = validator.Validate(new GetTenantByIdQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetTenantBySlugQueryValidator_Should_Fail_On_Empty_Slug()
    {
        var validator = new GetTenantBySlugQueryValidator();
        var result = validator.Validate(new GetTenantBySlugQuery(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    [Fact]
    public void GetTenantBySlugQueryValidator_Should_Fail_On_TooLong_Slug()
    {
        var validator = new GetTenantBySlugQueryValidator();
        var result = validator.Validate(new GetTenantBySlugQuery(new string('a', 101)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetTenantBySlugQueryValidator_Should_Pass_On_Valid_Slug()
    {
        var validator = new GetTenantBySlugQueryValidator();
        var result = validator.Validate(new GetTenantBySlugQuery("valid-slug"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetTenantMembersQueryValidator_Should_Fail_On_Empty_TenantId()
    {
        var validator = new GetTenantMembersQueryValidator();
        var result = validator.Validate(new GetTenantMembersQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void GetTenantMembersQueryValidator_Should_Pass_On_Valid_TenantId()
    {
        var validator = new GetTenantMembersQueryValidator();
        var result = validator.Validate(new GetTenantMembersQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetTenantsPageQueryValidator_Should_Fail_On_Invalid_PageNumber()
    {
        var validator = new GetTenantsPageQueryValidator();
        var result = validator.Validate(new GetTenantsPageQuery(0, 10));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetTenantsPageQueryValidator_Should_Fail_On_Invalid_PageSize()
    {
        var validator = new GetTenantsPageQueryValidator();
        var result = validator.Validate(new GetTenantsPageQuery(1, 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetTenantsPageQueryValidator_Should_Pass_On_Valid_Data()
    {
        var validator = new GetTenantsPageQueryValidator();
        var result = validator.Validate(new GetTenantsPageQuery(1, 10));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetUserMembershipsQueryValidator_Should_Fail_On_Empty_UserId()
    {
        var validator = new GetUserMembershipsQueryValidator();
        var result = validator.Validate(new GetUserMembershipsQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void GetUserMembershipsQueryValidator_Should_Pass_On_Valid_UserId()
    {
        var validator = new GetUserMembershipsQueryValidator();
        var result = validator.Validate(new GetUserMembershipsQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SearchTenantsQueryValidator_Should_Fail_On_Zero_MaxResponses()
    {
        var validator = new SearchTenantsQueryValidator();
        var result = validator.Validate(new SearchTenantsQuery(MaxResponses: 0));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SearchTenantsQueryValidator_Should_Pass_On_Valid_Data()
    {
        var validator = new SearchTenantsQueryValidator();
        var result = validator.Validate(new SearchTenantsQuery("search", null, null, null, null, null, 100));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SearchTenantsQueryValidator_Should_Pass_With_Default_Values()
    {
        var validator = new SearchTenantsQueryValidator();
        var result = validator.Validate(new SearchTenantsQuery());

        result.IsValid.Should().BeTrue();
    }
}
