using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.SharedKernel.UnitTests;

#region Address

public class AddressTests
{
    [Fact]
    public void Constructor_ValidArgs_SetsProperties()
    {
        var addr = new Address("123 Main St", "Springfield", "IL", "62701", "US");

        addr.Street.Should().Be("123 Main St");
        addr.City.Should().Be("Springfield");
        addr.State.Should().Be("IL");
        addr.PostalCode.Should().Be("62701");
        addr.Country.Should().Be("US");
        addr.Unit.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithUnit_SetsUnit()
    {
        var addr = new Address("123 Main St", "Springfield", "IL", "62701", "US", "4B");
        addr.Unit.Should().Be("4B");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var addr = new Address("  123 Main St  ", "  Springfield  ", "  IL  ", "  62701  ", "  US  ", "  4B  ");
        addr.Street.Should().Be("123 Main St");
        addr.Unit.Should().Be("4B");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyStreet_Throws(string? street)
    {
        var act = () => new Address(street!, "City", "ST", "12345", "US");
        act.Should().Throw<ArgumentException>().WithParameterName("street");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyCity_Throws(string? city)
    {
        var act = () => new Address("Street", city!, "ST", "12345", "US");
        act.Should().Throw<ArgumentException>().WithParameterName("city");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyState_Throws(string? state)
    {
        var act = () => new Address("Street", "City", state!, "12345", "US");
        act.Should().Throw<ArgumentException>().WithParameterName("state");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyPostalCode_Throws(string? postalCode)
    {
        var act = () => new Address("Street", "City", "ST", postalCode!, "US");
        act.Should().Throw<ArgumentException>().WithParameterName("postalCode");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyCountry_Throws(string? country)
    {
        var act = () => new Address("Street", "City", "ST", "12345", country!);
        act.Should().Throw<ArgumentException>().WithParameterName("country");
    }

    [Fact]
    public void GetFullAddress_WithoutUnit_ReturnsMultiLine()
    {
        var addr = new Address("123 Main St", "Springfield", "IL", "62701", "US");
        var full = addr.GetFullAddress();
        full.Should().Contain("123 Main St");
        full.Should().Contain("Springfield, IL 62701");
        full.Should().Contain("US");
        full.Should().NotContain("Unit");
    }

    [Fact]
    public void GetFullAddress_WithUnit_IncludesUnit()
    {
        var addr = new Address("123 Main St", "Springfield", "IL", "62701", "US", "4B");
        var full = addr.GetFullAddress();
        full.Should().Contain("Unit 4B");
    }

    [Fact]
    public void GetOneLine_WithoutUnit_ReturnsSingleLine()
    {
        var addr = new Address("123 Main St", "Springfield", "IL", "62701", "US");
        var line = addr.GetOneLine();
        line.Should().Be("123 Main St, Springfield, IL, 62701, US");
    }

    [Fact]
    public void GetOneLine_WithUnit_IncludesUnit()
    {
        var addr = new Address("123 Main St", "Springfield", "IL", "62701", "US", "4B");
        var line = addr.GetOneLine();
        line.Should().Contain("Unit 4B");
    }

    [Fact]
    public void ToString_DelegatesToGetOneLine()
    {
        var addr = new Address("123 Main St", "Springfield", "IL", "62701", "US");
        addr.ToString().Should().Be(addr.GetOneLine());
    }
}

#endregion

#region BusinessRuleViolationException

public class BusinessRuleViolationExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var ex = new BusinessRuleViolationException("MAX_USERS", "Too many users", new { Count = 100 });

        ex.Rule.Should().Be("MAX_USERS");
        ex.Message.Should().Be("Too many users");
        ex.Context.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullContext_IsAllowed()
    {
        var ex = new BusinessRuleViolationException("RULE", "message");

        ex.Context.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new BusinessRuleViolationException("RULE", "message", inner, new { Data = "test" });

        ex.InnerException.Should().Be(inner);
        ex.Rule.Should().Be("RULE");
        ex.Context.Should().NotBeNull();
    }

    [Fact]
    public void InheritsFromDomainException()
    {
        var ex = new BusinessRuleViolationException("RULE", "message");
        ex.Should().BeAssignableTo<DomainException>();
    }
}

#endregion

#region Records (UserInfo, TenantInfo)

public class UserInfoTests
{
    [Fact]
    public void CanBeCreated_WithAllParams()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new UserInfo(id, "user@example.com", "John Doe", true, tenantId);

        user.Id.Should().Be(id);
        user.Email.Should().Be("user@example.com");
        user.Name.Should().Be("John Doe");
        user.IsActive.Should().BeTrue();
        user.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void TenantId_DefaultsToNull()
    {
        var user = new UserInfo(Guid.NewGuid(), "a@b.com", "Test", true);
        user.TenantId.Should().BeNull();
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var id = Guid.NewGuid();
        var a = new UserInfo(id, "a@b.com", "Test", true);
        var b = new UserInfo(id, "a@b.com", "Test", true);
        a.Should().Be(b);
    }
}

public class TenantInfoTests
{
    [Fact]
    public void CanBeCreated()
    {
        var id = Guid.NewGuid();
        var tenant = new TenantInfo(id, "Acme Corp", "acme-corp", true);

        tenant.Id.Should().Be(id);
        tenant.Name.Should().Be("Acme Corp");
        tenant.Slug.Should().Be("acme-corp");
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var id = Guid.NewGuid();
        var a = new TenantInfo(id, "Acme", "acme", true);
        var b = new TenantInfo(id, "Acme", "acme", true);
        a.Should().Be(b);
    }
}

#endregion

#region SystemClock

public class SystemClockAdditionalTests
{
    [Fact]
    public void SetProvider_NullProvider_ThrowsArgumentNullException()
    {
        var act = () => SystemClock.SetProvider(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetProvider_CustomProvider_ChangesUtcNow()
    {
        try
        {
            var fakeTime = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
            var fakeProvider = new FakeTimeProvider(fakeTime);
            SystemClock.SetProvider(fakeProvider);

            SystemClock.UtcNow.Should().Be(fakeTime.UtcDateTime);
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public void Reset_RestoresSystemTime()
    {
        var fakeTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        SystemClock.SetProvider(new FakeTimeProvider(fakeTime));
        SystemClock.Reset();

        SystemClock.UtcNow.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    private sealed class FakeTimeProvider(DateTimeOffset time) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => time;
    }
}

#endregion

#region ModuleRegistry

public class ModuleRegistryTests
{
    [Fact]
    public void RegisterModule_Instance_AddsToModules()
    {
        var registry = new ModuleRegistry();
        var module = new TestModule("TestA");

        registry.RegisterModule(module);

        registry.Modules.Should().HaveCount(1);
        registry.Modules[0].Module.Name.Should().Be("TestA");
        registry.Modules[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void RegisterModule_Generic_WithConfiguration()
    {
        var registry = new ModuleRegistry();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();

        registry.RegisterModule<ParameterlessModule>(config);

        registry.Modules.Should().HaveCount(1);
        registry.Modules[0].Module.Name.Should().Be("Parameterless");
    }

    [Fact]
    public void RegisterModule_Generic_DisabledViaConfig()
    {
        var registry = new ModuleRegistry();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Modules:Parameterless:Enabled", "false" }
            })
            .Build();

        registry.RegisterModule<ParameterlessModule>(config);

        registry.Modules.Should().HaveCount(1);
        registry.Modules[0].IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void DiscoverModules_ScansAssembly()
    {
        var registry = new ModuleRegistry();
        var config = new ConfigurationBuilder().Build();

        // DiscoverModules scans for IModule types; even if some fail to instantiate
        // the method will process types it can find
        var act = () => registry.DiscoverModules([typeof(ModuleRegistry).Assembly], config);

        // SharedKernel assembly itself doesn't have concrete IModule implementations,
        // so it just returns without adding anything — no exception
        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterModule_Instance_Disabled()
    {
        var registry = new ModuleRegistry();
        var module = new TestModule("TestA");

        registry.RegisterModule(module, isEnabled: false);

        registry.Modules.Should().HaveCount(1);
        registry.Modules[0].IsEnabled.Should().BeFalse();
        registry.EnabledModules.Should().BeEmpty();
    }

    [Fact]
    public void RegisterModule_DuplicateType_Ignored()
    {
        var registry = new ModuleRegistry();
        var mod1 = new TestModule("Test1");
        var mod2 = new TestModule("Test2"); // same type

        registry.RegisterModule(mod1);
        registry.RegisterModule(mod2);

        registry.Modules.Should().HaveCount(1);
    }

    [Fact]
    public void RegisterModule_ByType_WithConfiguration()
    {
        var registry = new ModuleRegistry();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();

        registry.RegisterModule(typeof(ParameterlessModule), config);

        registry.Modules.Should().HaveCount(1);
    }

    [Fact]
    public void RegisterModule_ByType_NotIModule_Throws()
    {
        var registry = new ModuleRegistry();
        var config = new ConfigurationBuilder().Build();

        var act = () => registry.RegisterModule(typeof(string), config);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void RegisterModule_ByType_Duplicate_Ignored()
    {
        var registry = new ModuleRegistry();
        var config = new ConfigurationBuilder().Build();

        registry.RegisterModule(typeof(ParameterlessModule), config);
        registry.RegisterModule(typeof(ParameterlessModule), config);

        registry.Modules.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveDependencies_SealsRegistry()
    {
        var registry = new ModuleRegistry();
        registry.RegisterModule(new TestModule("A"));
        registry.ResolveDependencies();

        var act = () => registry.RegisterModule(new TestModule("B"));
        act.Should().Throw<InvalidOperationException>().WithMessage("*resolved*");
    }

    [Fact]
    public void EnabledModules_FiltersDisabled()
    {
        var registry = new ModuleRegistry();
        registry.RegisterModule(new TestModule("A"), isEnabled: true);
        registry.RegisterModule(new TestModule2("B"), isEnabled: false);

        registry.EnabledModules.Should().HaveCount(1);
        registry.EnabledModules.First().Module.Name.Should().Be("A");
    }

    [Fact]
    public void ConfigureServices_RegistersSelfAndCallsModules()
    {
        var registry = new ModuleRegistry();
        var module = new TestModule("A");
        registry.RegisterModule(module);

        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        registry.ConfigureServices(services, config);

        services.Should().Contain(d => d.ServiceType == typeof(ModuleRegistry));
        module.ConfigureServicesCalled.Should().BeTrue();
    }

    [Fact]
    public void LogBootstrapStatus_LogsModuleInfo()
    {
        var registry = new ModuleRegistry();
        registry.RegisterModule(new TestModule("A"));
        registry.RegisterModule(new TestModule2("B"), isEnabled: false);

        var logger = new Mock<ILogger>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        registry.LogBootstrapStatus(logger.Object);
        // Should not throw — verifying it completes
    }

    [Fact]
    public void ModuleDescriptor_ExposesProperties()
    {
        var module = new TestModule("Desc");
        var desc = new ModuleDescriptor(typeof(TestModule), module, true);

        desc.ModuleType.Should().Be(typeof(TestModule));
        desc.Module.Should().Be(module);
        desc.IsEnabled.Should().BeTrue();
    }

    // Test modules for ModuleRegistry tests
    private class TestModule(string name) : ModuleBase
    {
        public override string Name => name;
        public bool ConfigureServicesCalled { get; private set; }

        public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            ConfigureServicesCalled = true;
            return services;
        }
    }

    private class TestModule2(string name) : ModuleBase
    {
        public override string Name => name;

        public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
            => services;
    }

    private class ParameterlessModule : ModuleBase
    {
        public override string Name => "Parameterless";

        public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
            => services;
    }
}

#endregion

#region SecurityHeadersOptions

public class SecurityHeadersOptionsTests
{
    [Fact]
    public void Defaults_AreSecure()
    {
        var opts = new SecurityHeadersOptions();

        opts.EnableXContentTypeOptions.Should().BeTrue();
        opts.EnableXFrameOptions.Should().BeTrue();
        opts.XFrameOptionsValue.Should().Be("DENY");
        opts.EnableReferrerPolicy.Should().BeTrue();
        opts.ReferrerPolicyValue.Should().Be("strict-origin-when-cross-origin");
        opts.EnableXXssProtection.Should().BeTrue();
        opts.EnableContentSecurityPolicy.Should().BeTrue();
        opts.ContentSecurityPolicyValue.Should().Contain("default-src 'none'");
        opts.SwaggerContentSecurityPolicyValue.Should().Contain("default-src 'self'");
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var opts = new SecurityHeadersOptions
        {
            EnableXContentTypeOptions = false,
            EnableXFrameOptions = false,
            XFrameOptionsValue = "SAMEORIGIN",
            EnableReferrerPolicy = false,
            ReferrerPolicyValue = "no-referrer",
            EnableXXssProtection = false,
            EnableContentSecurityPolicy = false,
            ContentSecurityPolicyValue = "default-src *"
        };

        opts.EnableXContentTypeOptions.Should().BeFalse();
        opts.XFrameOptionsValue.Should().Be("SAMEORIGIN");
        opts.ReferrerPolicyValue.Should().Be("no-referrer");
        opts.ContentSecurityPolicyValue.Should().Be("default-src *");
    }
}

#endregion

#region ModuleBase

public class ModuleBaseTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var module = new ConcreteModule();

        module.Order.Should().Be(100);
        module.EnabledByDefault.Should().BeTrue();
        module.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void MapEndpoints_DefaultImplementation_ReturnsEndpoints()
    {
        var module = new ConcreteModule();
        var endpoints = new Mock<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder>();

        var result = module.MapEndpoints(endpoints.Object);
        result.Should().Be(endpoints.Object);
    }

    private class ConcreteModule : ModuleBase
    {
        public override string Name => "Concrete";

        public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
            => services;
    }
}

#endregion
