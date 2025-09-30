using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Abstractions;

/// <summary>
/// Unit tests for Core abstractions and interfaces
/// </summary>
public class CoreAbstractionsTests
{
    // Test entity to verify interfaces
    private class TestEntity : EntityBase<int>, IAuditable, IConcurrencyControlled, ITenantScoped
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void EntityBase_Should_Implement_IEntity()
    {
        // Arrange & Act
        TestEntity entity = new();

        // Assert
        _ = entity.Should().BeAssignableTo<IEntity<int>>();
    }

    [Fact]
    public void EntityBase_Should_Implement_IAuditable()
    {
        // Arrange & Act
        TestEntity entity = new();

        // Assert
        _ = entity.Should().BeAssignableTo<IAuditable>();
        _ = entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _ = entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void EntityBase_Should_Implement_IConcurrencyControlled()
    {
        // Arrange & Act
        TestEntity entity = new();

        // Assert
        _ = entity.Should().BeAssignableTo<IConcurrencyControlled>();
        _ = entity.Version.Should().Be(0);
    }

    [Fact]
    public void EntityBase_Should_Implement_ITenantScoped()
    {
        // Arrange & Act
        TestEntity entity = new();

        // Assert
        _ = entity.Should().BeAssignableTo<ITenantScoped>();
        _ = entity.Tenant.Should().BeNull();
        _ = entity.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void IDateTimeProvider_Interface_Should_Define_Required_Properties()
    {
        // Arrange
        DateTimeProvider provider = new();

        // Act & Assert
        _ = provider.Should().BeAssignableTo<IDateTimeProvider>();

        // Verify interface contract
        IDateTimeProvider interfaceProvider = provider;
        _ = interfaceProvider.UtcNow.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _ = interfaceProvider.Now.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        _ = interfaceProvider.Today.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }
}

/// <summary>
/// Tests for IResult interface implementations
/// </summary>
public class IResultTests
{
    [Fact]
    public void Result_Should_Implement_IResult()
    {
        // Arrange
        Result successResult = Result.Success();
        Result failureResult = Result.Failure(Error.Failure("Test", "Test error"));

        // Act & Assert
        _ = successResult.Should().BeAssignableTo<IResult>();
        _ = failureResult.Should().BeAssignableTo<IResult>();

        // Verify interface contract
        IResult iSuccessResult = successResult;
        IResult iFailureResult = failureResult;

        _ = iSuccessResult.IsSuccess.Should().BeTrue();
        _ = iSuccessResult.IsFailure.Should().BeFalse();
        _ = iFailureResult.IsSuccess.Should().BeFalse();
        _ = iFailureResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Result_Generic_Should_Implement_IResult_Generic()
    {
        // Arrange
        Result<string> successResult = Result.Success("test");
        Result<string> failureResult = Result.Failure<string>(Error.Failure("Test", "Test error"));

        // Act & Assert
        _ = successResult.Should().BeAssignableTo<IResult<string>>();
        _ = failureResult.Should().BeAssignableTo<IResult<string>>();

        // Verify interface contract
        IResult<string> iSuccessResult = successResult;
        IResult<string> iFailureResult = failureResult;

        _ = iSuccessResult.IsSuccess.Should().BeTrue();
        _ = iSuccessResult.Value.Should().Be("test");
        _ = iFailureResult.IsSuccess.Should().BeFalse();
        _ = iFailureResult.Value.Should().BeNull();
    }
}