using FluentAssertions;
using GameGuild.Commerce.Products;
using Moq;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Queries;

/// <summary>
/// Unit tests for CheckProductAccessQueryHandler
/// </summary>
public class CheckProductAccessQueryHandlerTests
{
    private readonly Mock<IUserProductRepository> _mockUserProductRepository;
    private readonly CheckProductAccessQueryHandler _handler;

    public CheckProductAccessQueryHandlerTests()
    {
        _mockUserProductRepository = new Mock<IUserProductRepository>();
        _handler = new CheckProductAccessQueryHandler(_mockUserProductRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenUserProductNotFound_ReturnsNoAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var query = new CheckProductAccessQuery(userId, productId);

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProduct?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.HasAccess.Should().BeFalse();
        result.AccessStatus.Should().BeNull();
        result.AccessEndDate.Should().BeNull();
        result.AcquisitionType.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserProductIsActive_ReturnsHasAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var query = new CheckProductAccessQuery(userId, productId);

        var userProduct = CreateUserProduct(
            userId: userId,
            productId: productId,
            accessStatus: ProductAccessStatus.Active,
            accessEndDate: DateTime.UtcNow.AddDays(30)
        );

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.HasAccess.Should().BeTrue();
        result.AccessStatus.Should().Be(ProductAccessStatus.Active);
    }

    [Fact]
    public async Task Handle_WhenAccessExpired_ReturnsNoAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var query = new CheckProductAccessQuery(userId, productId);

        var userProduct = CreateUserProduct(
            userId: userId,
            productId: productId,
            accessStatus: ProductAccessStatus.Active,
            accessEndDate: DateTime.UtcNow.AddDays(-1) // Expired yesterday
        );

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.HasAccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(ProductAccessStatus.Expired)]
    [InlineData(ProductAccessStatus.Revoked)]
    [InlineData(ProductAccessStatus.Suspended)]
    [InlineData(ProductAccessStatus.Cancelled)]
    public async Task Handle_WhenAccessStatusIsNotActive_ReturnsNoAccess(ProductAccessStatus status)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var query = new CheckProductAccessQuery(userId, productId);

        var userProduct = CreateUserProduct(
            userId: userId,
            productId: productId,
            accessStatus: status,
            accessEndDate: DateTime.UtcNow.AddDays(30)
        );

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.HasAccess.Should().BeFalse();
        result.AccessStatus.Should().Be(status);
    }

    [Fact]
    public async Task Handle_WhenAccessEndDateIsNull_TreatsAsUnlimited()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var query = new CheckProductAccessQuery(userId, productId);

        var userProduct = CreateUserProduct(
            userId: userId,
            productId: productId,
            accessStatus: ProductAccessStatus.Active,
            accessEndDate: null // No end date = unlimited
        );

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.HasAccess.Should().BeTrue();
        result.AccessEndDate.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Func<Task> act = async () => await _handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(ProductAcquisitionType.Purchase)]
    [InlineData(ProductAcquisitionType.Subscription)]
    [InlineData(ProductAcquisitionType.Grant)]
    [InlineData(ProductAcquisitionType.PromoCode)]
    [InlineData(ProductAcquisitionType.Trial)]
    [InlineData(ProductAcquisitionType.Free)]
    [InlineData(ProductAcquisitionType.Gift)]
    public async Task Handle_ReturnsCorrectAcquisitionType(ProductAcquisitionType acquisitionType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var query = new CheckProductAccessQuery(userId, productId);

        var userProduct = CreateUserProduct(
            userId: userId,
            productId: productId,
            accessStatus: ProductAccessStatus.Active,
            accessEndDate: DateTime.UtcNow.AddDays(30),
            acquisitionType: acquisitionType
        );

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProduct);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AcquisitionType.Should().Be(acquisitionType);
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var query = new CheckProductAccessQuery(userId, productId);
        var cancellationToken = new CancellationToken();

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, cancellationToken))
            .ReturnsAsync((UserProduct?)null);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockUserProductRepository.Verify(
            r => r.GetByUserAndProductAsync(userId, productId, cancellationToken),
            Times.Once);
    }

    private static UserProduct CreateUserProduct(
        Guid userId,
        Guid productId,
        ProductAccessStatus accessStatus,
        DateTime? accessEndDate,
        ProductAcquisitionType acquisitionType = ProductAcquisitionType.Purchase)
    {
        return new UserProduct
        {
            UserId = userId,
            ProductId = productId,
            AccessStatus = accessStatus,
            AccessEndDate = accessEndDate,
            AcquisitionType = acquisitionType
        };
    }
}
