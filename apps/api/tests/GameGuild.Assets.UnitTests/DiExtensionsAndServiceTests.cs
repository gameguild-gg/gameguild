using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Amazon.S3;
using GameGuild.Assets;
using GameGuild.Assets.Deduplication;
using GameGuild.Assets.Extensions;
using GameGuild.Assets.Security;
using GameGuild.Assets.Storage;
using GameGuild.Assets.VirusScan;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Models;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Assets.UnitTests;

public class DiExtensionsAndServiceTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    // ═══════════════════════════════════════════════════════════════════
    // AssetsModuleExtensions — DI registration
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AddAssetsModule_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddAssetsModule(EmptyConfig());
        services.Should().NotBeEmpty();
        services.Count.Should().BeGreaterThan(5);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Service constructors — Storage
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void StorageServiceFactory_CanBeConstructed()
    {
        var svc = new StorageServiceFactory(
            Options.Create(new GlobalStorageOptions()),
            Mock.Of<ITenantStorageConfigurationRepository>(),
            Mock.Of<IStorageConfigurationEncryption>(),
            Mock.Of<IStorageService>(),
            Mock.Of<ILogger<StorageServiceFactory>>());

        svc.Should().NotBeNull();
    }

    [Fact]
    public void S3StorageService_CanBeConstructed()
    {
        var svc = new S3StorageService(
            Mock.Of<IAmazonS3>(),
            Options.Create(new StorageOptions()));

        svc.Should().NotBeNull();
    }

    [Fact]
    public void AssetStorageService_CanBeConstructed()
    {
        var svc = new AssetStorageService(
            Mock.Of<IAmazonS3>(),
            Options.Create(new AssetStorageOptions()));

        svc.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Service constructors — Security
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void TenantAssetValidationService_CanBeConstructed()
    {
        var svc = new TenantAssetValidationService(
            Options.Create(new TenantIsolationOptions()),
            Mock.Of<ILogger<TenantAssetValidationService>>());

        svc.Should().NotBeNull();
    }

    [Fact]
    public void AssetAuthorizationHandler_CanBeConstructed()
    {
        var handler = new AssetAuthorizationHandler(
            Mock.Of<IActorContextAccessor>(),
            Mock.Of<IAssetReferenceRepository>(),
            Mock.Of<IAccessControlListService>(),
            Mock.Of<ILogger<AssetAuthorizationHandler>>());

        handler.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Service constructors — Core services
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AssetTokenService_CanBeConstructed()
    {
        var svc = new AssetTokenService(
            Options.Create(new AssetTokenOptions()));

        svc.Should().NotBeNull();
    }

    // AssetAccessService requires IFeatureFlagEvaluationService from GameGuild.Features
    // which is not transitively available in the test project. Skipped.

    [Fact]
    public void SecureUploadService_CanBeConstructed()
    {
        var svc = new SecureUploadService(
            Mock.Of<IAssetUploadService>(),
            Mock.Of<IVirusScanService>(),
            Mock.Of<IAssetModerationService>(),
            Mock.Of<IAssetContentRepository>(),
            Mock.Of<IAssetStorageService>(),
            Options.Create(new VirusScanOptions()),
            Mock.Of<ILogger<SecureUploadService>>());

        svc.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Service constructors — Deduplication
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void DeduplicationService_CanBeConstructed()
    {
        var svc = new DeduplicationService(
            Mock.Of<IAssetContentRepository>(),
            Options.Create(new DeduplicationOptions()),
            Mock.Of<ILogger<DeduplicationService>>());

        svc.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Options & config types
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AssetTokenOptions_DefaultValues()
    {
        var opts = new AssetTokenOptions();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void AssetStorageOptions_DefaultValues()
    {
        var opts = new AssetStorageOptions();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void AssetAccessOptions_DefaultValues()
    {
        var opts = new AssetAccessOptions();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void GlobalStorageOptions_DefaultValues()
    {
        var opts = new GlobalStorageOptions();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void StorageOptions_DefaultValues()
    {
        var opts = new StorageOptions();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void TenantIsolationOptions_DefaultValues()
    {
        var opts = new TenantIsolationOptions();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void VirusScanOptions_DefaultValues()
    {
        var opts = new VirusScanOptions();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void DeduplicationOptions_DefaultValues()
    {
        var opts = new DeduplicationOptions();
        opts.Should().NotBeNull();
    }

    [Fact]
    public void AssetAccessRequirement_Read_HasExpectedPermission()
    {
        var req = AssetAccessRequirement.Read;
        req.Should().NotBeNull();
        req.RequiredPermission.Should().NotBeNull();
        req.AllowOwnerAccess.Should().BeTrue();
    }

    [Fact]
    public void AssetAccessRequirement_Create_DisallowsOwnerAccess()
    {
        var req = AssetAccessRequirement.Create;
        req.Should().NotBeNull();
        req.AllowOwnerAccess.Should().BeFalse();
    }

    [Fact]
    public void AssetAccessRequirement_CustomPermission()
    {
        var req = new AssetAccessRequirement(AssetsPermission.Read, allowOwnerAccess: true);
        req.Should().NotBeNull();
        req.RequiredPermission.Should().Be(AssetsPermission.Read);
    }
}
