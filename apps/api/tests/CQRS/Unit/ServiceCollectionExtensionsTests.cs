using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Tests.CQRS.Unit;

/// <summary>
/// Unit tests for CQRS ServiceCollectionExtensions
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCqrs_Should_Register_Core_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCqrs(assembly);

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<ServiceFactory>().Should().NotBeNull();
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
        serviceProvider.GetService<ISender>().Should().NotBeNull();
        serviceProvider.GetService<IPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddCqrs_Should_Use_Calling_Assembly_When_No_Assemblies_Provided()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddCqrs();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddCqrs_Should_Accept_Configuration_Action()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();
        var configurationCalled = false;

        // Act
        services.AddCqrs(config =>
        {
            configurationCalled = true;
            config.Should().NotBeNull();
        }, assembly);

        // Assert
        configurationCalled.Should().BeTrue();
    }

    [Fact]
    public void AddCqrs_Should_ThrowArgumentNullException_When_Services_IsNull()
    {
        // Act & Assert
        var act = () => ((IServiceCollection)null!).AddCqrs(Assembly.GetExecutingAssembly());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCqrs_Should_ThrowArgumentNullException_When_Assemblies_IsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddCqrs((Assembly[])null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCqrs_Should_Register_Handlers_From_Assembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCqrs(assembly);

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify that handlers would be registered (we can't test actual handler registration 
        // without concrete handler implementations in test assembly)
        var mediator = serviceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
        mediator.Should().BeOfType<Mediator>();
    }

    [Fact]
    public void AddCqrs_Should_Register_Multiple_Assemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly1 = Assembly.GetExecutingAssembly();
        var assembly2 = typeof(Mediator).Assembly;

        // Act
        services.AddCqrs(assembly1, assembly2);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddCqrs_Should_Register_ServiceFactory_As_Scoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCqrs(assembly);

        // Assert
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ServiceFactory));
        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddCqrs_Should_Register_IMediator_As_Scoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCqrs(assembly);

        // Assert
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        serviceDescriptor.ImplementationType.Should().Be<Mediator>();
    }

    [Fact]
    public void AddCqrs_Should_Register_ISender_As_Scoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCqrs(assembly);

        // Assert
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISender));
        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddCqrs_Should_Register_IPublisher_As_Scoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCqrs(assembly);

        // Assert
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPublisher));
        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddCqrs_Should_Not_Register_Duplicate_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCqrs(assembly);
        services.AddCqrs(assembly); // Add again

        // Assert
        var mediatorServices = services.Where(s => s.ServiceType == typeof(IMediator)).ToList();
        mediatorServices.Should().HaveCount(1);
    }
}