using System.Linq.Expressions;
using FluentAssertions;

namespace GameGuild.SharedKernel.UnitTests.Specifications;

// Concrete subclass for testing the abstract Specification<T>
public class TestSpecification<T> : Specification<T>
{
    public TestSpecification() { }
    public TestSpecification(Expression<Func<T, bool>> criteria) : base(criteria) { }

    // Expose protected methods for testing
    public void TestAddInclude(Expression<Func<T, object>> include) => AddInclude(include);
    public void TestAddIncludeString(string include) => AddInclude(include);
    public void TestApplyOrderBy(Expression<Func<T, object>> expr) => ApplyOrderBy(expr);
    public void TestApplyOrderByDescending(Expression<Func<T, object>> expr) => ApplyOrderByDescending(expr);
    public void TestApplyGroupBy(Expression<Func<T, object>> expr) => ApplyGroupBy(expr);
    public void TestApplyPaging(int skip, int take) => ApplyPaging(skip, take);
    public void TestApplyCriteria(Expression<Func<T, bool>> criteria) => ApplyCriteria(criteria);
    public void TestIncludeDeletedEntities() => IncludeDeletedEntities();
    public void TestEnableSplitQuery() => EnableSplitQuery();
    public void TestEnableAsNoTracking() => EnableAsNoTracking();
    public void TestEnableAsNoTrackingWithIdentityResolution() => EnableAsNoTrackingWithIdentityResolution();
}

public record TestEntity(int Id, string Name, string Category);

public class SpecificationTests
{
    [Fact]
    public void Default_ShouldHaveNoCriteria()
    {
        var spec = new TestSpecification<TestEntity>();

        spec.Criteria.Should().BeNull();
        spec.Includes.Should().BeEmpty();
        spec.IncludeStrings.Should().BeEmpty();
        spec.OrderBy.Should().BeNull();
        spec.OrderByDescending.Should().BeNull();
        spec.GroupBy.Should().BeNull();
        spec.IsPagingEnabled.Should().BeFalse();
        spec.IncludeDeleted.Should().BeFalse();
        spec.SplitQuery.Should().BeFalse();
        spec.AsNoTracking.Should().BeFalse();
        spec.AsNoTrackingWithIdentityResolution.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCriteria_ShouldSetCriteria()
    {
        Expression<Func<TestEntity, bool>> criteria = e => e.Name == "Test";
        var spec = new TestSpecification<TestEntity>(criteria);

        spec.Criteria.Should().NotBeNull();
        spec.Criteria.Should().BeSameAs(criteria);
    }

    [Fact]
    public void ApplyCriteria_ShouldOverridePrevious()
    {
        var spec = new TestSpecification<TestEntity>(e => e.Id == 1);
        Expression<Func<TestEntity, bool>> newCriteria = e => e.Id == 2;

        spec.TestApplyCriteria(newCriteria);

        spec.Criteria.Should().BeSameAs(newCriteria);
    }

    [Fact]
    public void AddInclude_Expression_ShouldAddToCollection()
    {
        var spec = new TestSpecification<TestEntity>();
        Expression<Func<TestEntity, object>> include = e => e.Name;

        spec.TestAddInclude(include);

        spec.Includes.Should().HaveCount(1);
        spec.Includes[0].Should().BeSameAs(include);
    }

    [Fact]
    public void AddInclude_String_ShouldAddToCollection()
    {
        var spec = new TestSpecification<TestEntity>();

        spec.TestAddIncludeString("Items.SubItems");

        spec.IncludeStrings.Should().ContainSingle().Which.Should().Be("Items.SubItems");
    }

    [Fact]
    public void AddMultipleIncludes_ShouldAccumulate()
    {
        var spec = new TestSpecification<TestEntity>();

        spec.TestAddInclude(e => e.Name);
        spec.TestAddInclude(e => e.Category);
        spec.TestAddIncludeString("Related");

        spec.Includes.Should().HaveCount(2);
        spec.IncludeStrings.Should().HaveCount(1);
    }

    [Fact]
    public void ApplyOrderBy_ShouldSet()
    {
        var spec = new TestSpecification<TestEntity>();
        Expression<Func<TestEntity, object>> orderBy = e => e.Name;

        spec.TestApplyOrderBy(orderBy);

        spec.OrderBy.Should().BeSameAs(orderBy);
        spec.OrderByDescending.Should().BeNull();
    }

    [Fact]
    public void ApplyOrderByDescending_ShouldSet()
    {
        var spec = new TestSpecification<TestEntity>();
        Expression<Func<TestEntity, object>> orderByDesc = e => e.Id;

        spec.TestApplyOrderByDescending(orderByDesc);

        spec.OrderByDescending.Should().BeSameAs(orderByDesc);
        spec.OrderBy.Should().BeNull();
    }

    [Fact]
    public void ApplyGroupBy_ShouldSet()
    {
        var spec = new TestSpecification<TestEntity>();
        Expression<Func<TestEntity, object>> groupBy = e => e.Category;

        spec.TestApplyGroupBy(groupBy);

        spec.GroupBy.Should().BeSameAs(groupBy);
    }

    [Fact]
    public void ApplyPaging_ShouldSetSkipTakeAndEnable()
    {
        var spec = new TestSpecification<TestEntity>();

        spec.TestApplyPaging(10, 25);

        spec.Skip.Should().Be(10);
        spec.Take.Should().Be(25);
        spec.IsPagingEnabled.Should().BeTrue();
    }

    [Fact]
    public void IncludeDeletedEntities_ShouldSetFlag()
    {
        var spec = new TestSpecification<TestEntity>();

        spec.TestIncludeDeletedEntities();

        spec.IncludeDeleted.Should().BeTrue();
    }

    [Fact]
    public void EnableSplitQuery_ShouldSetFlag()
    {
        var spec = new TestSpecification<TestEntity>();

        spec.TestEnableSplitQuery();

        spec.SplitQuery.Should().BeTrue();
    }

    [Fact]
    public void EnableAsNoTracking_ShouldSetFlag()
    {
        var spec = new TestSpecification<TestEntity>();

        spec.TestEnableAsNoTracking();

        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void EnableAsNoTrackingWithIdentityResolution_ShouldSetFlag()
    {
        var spec = new TestSpecification<TestEntity>();

        spec.TestEnableAsNoTrackingWithIdentityResolution();

        spec.AsNoTrackingWithIdentityResolution.Should().BeTrue();
    }

    [Fact]
    public void Includes_ShouldBeReadOnly()
    {
        var spec = new TestSpecification<TestEntity>();

        spec.Includes.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyCollection<Expression<Func<TestEntity, object>>>>();
        spec.IncludeStrings.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyCollection<string>>();
    }
}
