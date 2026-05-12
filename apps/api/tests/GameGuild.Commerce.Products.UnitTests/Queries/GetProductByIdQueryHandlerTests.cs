using FluentAssertions;
using GameGuild;
using GameGuild.Commerce.Products;
using Moq;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Queries;

/// <summary>
/// Unit tests for GetProductByIdQueryHandler
/// </summary>
public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _handler = new GetProductByIdQueryHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenProductExists_ReturnsProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId);
        var product = CreateProduct(productId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), true, false, true))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), true, false, true))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MapsProductFieldsCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId);

        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "A test product description",
            ShortDescription = "Short desc",
            ImageUrl = "https://example.com/image.png",
            Type = ProductType.Course,
            IsBundle = false,
            CreatorId = creatorId,
            Pricing = new List<ProductPricing>()
        };
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(product, new DateTime(2024, 1, 1));
        typeof(EntityBase).GetProperty(nameof(EntityBase.UpdatedAt))!.SetValue(product, new DateTime(2024, 6, 1));

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), true, false, true))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Product");
        result.Description.Should().Be("A test product description");
        result.ShortDescription.Should().Be("Short desc");
        result.ImageUrl.Should().Be("https://example.com/image.png");
        result.Type.Should().Be(ProductType.Course);
        result.IsBundle.Should().BeFalse();
        result.IsPublished.Should().BeTrue();
        result.CreatorId.Should().Be(creatorId);
    }

    [Fact]
    public async Task Handle_WithIncludePricingTrue_IncludesPricing()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId, IncludePricing: true);
        var product = CreateProductWithPricing(productId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), true, false, true))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Pricing.Should().NotBeNull();
        result.Pricing.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithIncludePricingFalse_ExcludesPricing()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId, IncludePricing: false);
        var product = CreateProductWithPricing(productId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false, true))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Pricing.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithIncludeCreatorTrue_PassesToRepository()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId, IncludeCreator: true);
        var product = CreateProduct(productId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), true, true, true))
            .ReturnsAsync(product);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), true, true, true), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIncludeUnpublishedTrue_DoesNotApplyPublishedFilter()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId, IncludeUnpublished: true);
        var product = CreateProduct(productId);
        product.IsPublished = false;

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), true, false, null))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsPublished.Should().BeFalse();
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), true, false, null), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId);
        using var cts = new CancellationTokenSource();

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, cts.Token, true, false, true))
            .ReturnsAsync(CreateProduct(productId));

        // Act
        await _handler.Handle(query, cts.Token);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(productId, cts.Token, true, false, true), Times.Once);
    }

    private static Product CreateProduct(Guid productId)
    {
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Type = ProductType.Course,
            Pricing = new List<ProductPricing>()
        };
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(product, DateTime.UtcNow);
        typeof(EntityBase).GetProperty(nameof(EntityBase.UpdatedAt))!.SetValue(product, DateTime.UtcNow);
        return product;
    }

    private static Product CreateProductWithPricing(Guid productId)
    {
        var (pricing, _) = ProductPricing.CreateWithVersion(
            productId: productId,
            name: "Standard",
            basePrice: 99.99m,
            currency: "USD",
            salePrice: null,
            saleStartDate: null,
            saleEndDate: null,
            isDefault: true
        );

        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Type = ProductType.Course,
            Pricing = new List<ProductPricing> { pricing }
        };
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(product, DateTime.UtcNow);
        typeof(EntityBase).GetProperty(nameof(EntityBase.UpdatedAt))!.SetValue(product, DateTime.UtcNow);
        return product;
    }
}
