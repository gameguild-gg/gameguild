namespace GameGuild.Assets.UnitTests;

public class AssetTokenServiceTests
{
    private readonly AssetTokenService _tokenService;

    public AssetTokenServiceTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new AssetTokenOptions
        {
            SecretKey = Convert.ToBase64String(new byte[32]), // 32 bytes for HMAC-SHA256
            DefaultExpiryHours = 24,
            TimeWindowHours = 8
        });
        _tokenService = new AssetTokenService(options);
    }

    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyToken()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var token = _tokenService.GenerateToken(
            assetId,
            tenantId,
            AssetAccessPolicy.Public);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_ShouldReturnDifferentTokensForDifferentAssets()
    {
        // Arrange
        var assetId1 = Guid.NewGuid();
        var assetId2 = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var token1 = _tokenService.GenerateToken(assetId1, tenantId, AssetAccessPolicy.Public);
        var token2 = _tokenService.GenerateToken(assetId2, tenantId, AssetAccessPolicy.Public);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnPayload()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = _tokenService.GenerateToken(
            assetId,
            tenantId,
            AssetAccessPolicy.Public);

        // Act
        var payload = _tokenService.ValidateToken(token, assetId, tenantId);

        // Assert
        payload.Should().NotBeNull();
        payload!.AssetReferenceId.Should().Be(assetId);
        payload.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void ValidateToken_WithInvalidAssetId_ShouldReturnNull()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var wrongAssetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var token = _tokenService.GenerateToken(assetId, tenantId, AssetAccessPolicy.Public);

        // Act
        var payload = _tokenService.ValidateToken(token, wrongAssetId, tenantId);

        // Assert
        payload.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithMalformedToken_ShouldReturnNull()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var payload = _tokenService.ValidateToken("invalid-token", assetId, tenantId);

        // Assert
        payload.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ShouldReturnNull()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var payload = _tokenService.ValidateToken(string.Empty, assetId, tenantId);

        // Assert
        payload.Should().BeNull();
    }

    [Fact]
    public void GetCurrentTimeWindow_ShouldReturnConsistentValue()
    {
        // Act
        var window1 = _tokenService.GetCurrentTimeWindow();
        var window2 = _tokenService.GetCurrentTimeWindow();

        // Assert
        window1.Should().Be(window2);
    }

    [Fact]
    public void GenerateToken_WithTransformation_ShouldWork()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var transformation = new TransformationSpec(100, 100, ImageFit.Cover, ImageFormat.Webp, 80);

        // Act
        var token = _tokenService.GenerateToken(
            assetId,
            tenantId,
            AssetAccessPolicy.Authenticated,
            transformation);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_WithCustomExpiry_ShouldWork()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var token = _tokenService.GenerateToken(
            assetId,
            tenantId,
            AssetAccessPolicy.Public,
            customExpiry: TimeSpan.FromMinutes(30));

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        // Token should still be valid
        var payload = _tokenService.ValidateToken(token, assetId, tenantId);
        payload.Should().NotBeNull();
    }
}
