using FluentAssertions;
using GameGuild.Commerce.Products;
using Moq;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Queries;

/// <summary>
/// Unit tests for GetUserProductsQueryHandler
/// </summary>
public class GetUserProductsQueryHandlerTests
{
    private readonly Mock<IUserProductRepository> _mockRepository;
    private readonly GetUserProductsQueryHandler _handler;

    public GetUserProductsQueryHandlerTests()
    {
        _mockRepository = new Mock<IUserProductRepository>();
        _handler = new GetUserProductsQueryHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenUserHasProducts_ReturnsProducts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProductsQuery(userId);
        var userProducts = new List<UserProduct>
        {
            CreateUserProduct(userId, ProductAccessStatus.Active),
            CreateUserProduct(userId, ProductAccessStatus.Active)
        };

        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProducts);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoProducts_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProductsQuery(userId);

        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProduct>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsPropertiesToDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var query = new GetUserProductsQuery(userId);

        var userProduct = new UserProduct
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            AccessStatus = ProductAccessStatus.Active,
            AcquisitionType = ProductAcquisitionType.Purchase,
            PricePaid = 99.99m,
            Currency = "USD",
            AccessStartDate = new DateTime(2024, 1, 1),
            AccessEndDate = new DateTime(2024, 12, 31),
            CreatedAt = new DateTime(2024, 1, 1)
        };

        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProduct> { userProduct });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.UserId.Should().Be(userId);
        dto.ProductId.Should().Be(productId);
        dto.AccessStatus.Should().Be(ProductAccessStatus.Active);
        dto.AcquisitionType.Should().Be(ProductAcquisitionType.Purchase);
        dto.PricePaid.Should().Be(99.99m);
        dto.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_WithStatusFilter_PassesFilterToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProductsQuery(userId, ProductAccessStatus.Active);

        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId, ProductAccessStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProduct>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            r => r.GetByUserIdAsync(userId, ProductAccessStatus.Active, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(ProductAccessStatus.Active)]
    [InlineData(ProductAccessStatus.Expired)]
    [InlineData(ProductAccessStatus.Revoked)]
    [InlineData(ProductAccessStatus.Suspended)]
    public async Task Handle_WithVariousStatusFilters_FiltersCorrectly(ProductAccessStatus status)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProductsQuery(userId, status);

        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId, status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProduct>
            {
                CreateUserProduct(userId, status)
            });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().AccessStatus.Should().Be(status);
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
        var userId = Guid.NewGuid();
        var query = new GetUserProductsQuery(userId);
        using var cts = new CancellationTokenSource();

        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId, null, cts.Token))
            .ReturnsAsync(new List<UserProduct>());

        // Act
        await _handler.Handle(query, cts.Token);

        // Assert
        _mockRepository.Verify(r => r.GetByUserIdAsync(userId, null, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleProducts_PreservesOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProductsQuery(userId);

        var productIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var userProducts = productIds.Select(pid => CreateUserProduct(userId, ProductAccessStatus.Active, pid)).ToList();

        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProducts);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Select(r => r.ProductId).Should().BeEquivalentTo(productIds, opts => opts.WithStrictOrdering());
    }

    private static UserProduct CreateUserProduct(
        Guid userId,
        ProductAccessStatus status,
        Guid? productId = null)
    {
        return new UserProduct
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId ?? Guid.NewGuid(),
            AccessStatus = status,
            AcquisitionType = ProductAcquisitionType.Purchase,
            PricePaid = 50m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow
        };
    }
}
