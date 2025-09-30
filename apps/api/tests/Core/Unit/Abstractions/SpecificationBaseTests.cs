using System.Linq.Expressions;
using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Abstractions;

/// <summary>
/// Unit tests for SpecificationBase
/// </summary>
public class SpecificationBaseTests
{
    // Test entity for specifications
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    // Test specification implementation
    private class TestSpecification : SpecificationBase<TestEntity>
    {
        public TestSpecification(Expression<Func<TestEntity, bool>> criteria) : base(criteria) { }

        public void TestAddInclude(Expression<Func<TestEntity, object>> includeExpression)
        {
            AddInclude(includeExpression);
        }

        public void TestAddInclude(string includeString)
        {
            AddInclude(includeString);
        }

        public void TestApplyOrderBy(Expression<Func<TestEntity, object>> orderByExpression)
        {
            ApplyOrderBy(orderByExpression);
        }

        public void TestApplyOrderByDescending(Expression<Func<TestEntity, object>> orderByDescendingExpression)
        {
            ApplyOrderByDescending(orderByDescendingExpression);
        }

        public void TestApplyGroupBy(Expression<Func<TestEntity, object>> groupByExpression)
        {
            ApplyGroupBy(groupByExpression);
        }

        public void TestApplyPaging(int skip, int take)
        {
            ApplyPaging(skip, take);
        }

        public void TestIncludeDeletedEntities()
        {
            IncludeDeletedEntities();
        }

        public void TestEnableSplitQuery()
        {
            EnableSplitQuery();
        }

        public void TestEnableAsNoTracking()
        {
            EnableAsNoTracking();
        }

        public void TestEnableAsNoTrackingWithIdentityResolution()
        {
            EnableAsNoTrackingWithIdentityResolution();
        }
    }

    [Fact]
    public void Constructor_Should_Set_Criteria()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;

        // Act
        TestSpecification specification = new(criteria);

        // Assert
        _ = specification.Criteria.Should().Be(criteria);
    }

    [Fact]
    public void Default_Values_Should_Be_Correct()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;

        // Act
        TestSpecification specification = new(criteria);

        // Assert
        _ = specification.Includes.Should().BeEmpty();
        _ = specification.IncludeStrings.Should().BeEmpty();
        _ = specification.OrderBy.Should().BeNull();
        _ = specification.OrderByDescending.Should().BeNull();
        _ = specification.GroupBy.Should().BeNull();
        _ = specification.IncludeDeleted.Should().BeFalse();
        _ = specification.Take.Should().Be(0);
        _ = specification.Skip.Should().Be(0);
        _ = specification.IsPagingEnabled.Should().BeFalse();
        _ = specification.SplitQuery.Should().BeFalse();
        _ = specification.AsNoTracking.Should().BeFalse();
        _ = specification.AsNoTrackingWithIdentityResolution.Should().BeFalse();
    }

    [Fact]
    public void AddInclude_Expression_Should_Add_To_Includes_Collection()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);
        Expression<Func<TestEntity, object>> includeExpression = x => x.Name;

        // Act
        specification.TestAddInclude(includeExpression);

        // Assert
        _ = specification.Includes.Should().HaveCount(1);
        _ = specification.Includes.Should().Contain(includeExpression);
    }

    [Fact]
    public void AddInclude_String_Should_Add_To_IncludeStrings_Collection()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);
        const string includeString = "RelatedEntity";

        // Act
        specification.TestAddInclude(includeString);

        // Assert
        _ = specification.IncludeStrings.Should().HaveCount(1);
        _ = specification.IncludeStrings.Should().Contain(includeString);
    }

    [Fact]
    public void ApplyOrderBy_Should_Set_OrderBy_Expression()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);
        Expression<Func<TestEntity, object>> orderByExpression = x => x.Name;

        // Act
        specification.TestApplyOrderBy(orderByExpression);

        // Assert
        _ = specification.OrderBy.Should().Be(orderByExpression);
    }

    [Fact]
    public void ApplyOrderByDescending_Should_Set_OrderByDescending_Expression()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);
        Expression<Func<TestEntity, object>> orderByDescendingExpression = x => x.CreatedAt;

        // Act
        specification.TestApplyOrderByDescending(orderByDescendingExpression);

        // Assert
        _ = specification.OrderByDescending.Should().Be(orderByDescendingExpression);
    }

    [Fact]
    public void ApplyGroupBy_Should_Set_GroupBy_Expression()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);
        Expression<Func<TestEntity, object>> groupByExpression = x => x.Value;

        // Act
        specification.TestApplyGroupBy(groupByExpression);

        // Assert
        _ = specification.GroupBy.Should().Be(groupByExpression);
    }

    [Fact]
    public void ApplyPaging_Should_Set_Paging_Properties()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);
        const int skip = 10;
        const int take = 20;

        // Act
        specification.TestApplyPaging(skip, take);

        // Assert
        _ = specification.Skip.Should().Be(skip);
        _ = specification.Take.Should().Be(take);
        _ = specification.IsPagingEnabled.Should().BeTrue();
    }

    [Fact]
    public void IncludeDeletedEntities_Should_Set_IncludeDeleted_To_True()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);

        // Act
        specification.TestIncludeDeletedEntities();

        // Assert
        _ = specification.IncludeDeleted.Should().BeTrue();
    }

    [Fact]
    public void EnableSplitQuery_Should_Set_SplitQuery_To_True()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);

        // Act
        specification.TestEnableSplitQuery();

        // Assert
        _ = specification.SplitQuery.Should().BeTrue();
    }

    [Fact]
    public void EnableAsNoTracking_Should_Set_AsNoTracking_To_True()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);

        // Act
        specification.TestEnableAsNoTracking();

        // Assert
        _ = specification.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void EnableAsNoTrackingWithIdentityResolution_Should_Set_Property_To_True()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);

        // Act
        specification.TestEnableAsNoTrackingWithIdentityResolution();

        // Assert
        _ = specification.AsNoTrackingWithIdentityResolution.Should().BeTrue();
    }

    [Fact]
    public void Multiple_Includes_Should_Be_Added_To_Collection()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);
        Expression<Func<TestEntity, object>> include1 = x => x.Name;
        Expression<Func<TestEntity, object>> include2 = x => x.Value;
        const string includeString1 = "RelatedEntity1";
        const string includeString2 = "RelatedEntity2";

        // Act
        specification.TestAddInclude(include1);
        specification.TestAddInclude(include2);
        specification.TestAddInclude(includeString1);
        specification.TestAddInclude(includeString2);

        // Assert
        _ = specification.Includes.Should().HaveCount(2);
        _ = specification.Includes.Should().Contain(include1);
        _ = specification.Includes.Should().Contain(include2);
        _ = specification.IncludeStrings.Should().HaveCount(2);
        _ = specification.IncludeStrings.Should().Contain(includeString1);
        _ = specification.IncludeStrings.Should().Contain(includeString2);
    }

    [Fact]
    public void Collections_Should_Be_ReadOnly()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;
        TestSpecification specification = new(criteria);

        // Act & Assert
        _ = specification.Includes.Should().BeAssignableTo<System.Collections.ObjectModel.ReadOnlyCollection<Expression<Func<TestEntity, object>>>>();
        _ = specification.IncludeStrings.Should().BeAssignableTo<System.Collections.ObjectModel.ReadOnlyCollection<string>>();
    }

    [Fact]
    public void Should_Implement_ISpecification()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive;

        // Act
        TestSpecification specification = new(criteria);

        // Assert
        _ = specification.Should().BeAssignableTo<ISpecification<TestEntity>>();
    }

    [Fact]
    public void Complex_Specification_Should_Work()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = x => x.IsActive && x.Value > 0;
        TestSpecification specification = new(criteria);

        // Act - Build a complex specification
        specification.TestAddInclude(x => x.Name);
        specification.TestAddInclude("RelatedEntity");
        specification.TestApplyOrderBy(x => x.CreatedAt);
        specification.TestApplyPaging(10, 20);
        specification.TestIncludeDeletedEntities();
        specification.TestEnableSplitQuery();
        specification.TestEnableAsNoTracking();

        // Assert
        _ = specification.Criteria.Should().Be(criteria);
        _ = specification.Includes.Should().HaveCount(1);
        _ = specification.IncludeStrings.Should().HaveCount(1);
        _ = specification.OrderBy.Should().NotBeNull();
        _ = specification.Skip.Should().Be(10);
        _ = specification.Take.Should().Be(20);
        _ = specification.IsPagingEnabled.Should().BeTrue();
        _ = specification.IncludeDeleted.Should().BeTrue();
        _ = specification.SplitQuery.Should().BeTrue();
        _ = specification.AsNoTracking.Should().BeTrue();
    }
}