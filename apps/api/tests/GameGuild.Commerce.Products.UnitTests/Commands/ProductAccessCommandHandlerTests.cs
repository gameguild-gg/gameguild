using FluentAssertions;
using GameGuild;
using GameGuild.Commerce.Products;
using GameGuild.CQRS;
using Moq;
using Xunit;
using CqrsUnit = GameGuild.CQRS.Unit;

namespace GameGuild.Tests.Commerce.Products.Unit.Commands;

/// <summary>
/// Unit tests for GrantProductAccessCommandHandler
/// </summary>
public class GrantProductAccessCommandHandlerTests
{
    private readonly Mock<IUserProductRepository> _mockUserProductRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly GrantProductAccessCommandHandler _handler;

    public GrantProductAccessCommandHandlerTests()
    {
        _mockUserProductRepository = new Mock<IUserProductRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _handler = new GrantProductAccessCommandHandler(
            _mockUserProductRepository.Object,
            _mockProductRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsProductNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new GrantProductAccessCommand(userId, productId);

        _mockProductRepository
            .Setup(r => r.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyHasAccess_UpdatesExistingAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new GrantProductAccessCommand(
            userId,
            productId,
            AcquisitionType: ProductAcquisitionType.Purchase,
            PricePaid: 100m,
            Currency: "USD");

        var existingAccess = CreateUserProduct(userId, productId, ProductAccessStatus.Expired);

        _mockProductRepository
            .Setup(r => r.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccess);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessStatus.Should().Be(ProductAccessStatus.Active);
        _mockUserProductRepository.Verify(r => r.UpdateAsync(existingAccess, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserProductRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAccess_CreatesNewAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new GrantProductAccessCommand(
            userId,
            productId,
            AcquisitionType: ProductAcquisitionType.Grant,
            PricePaid: 0,
            Currency: "USD");

        _mockProductRepository
            .Setup(r => r.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProduct?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.ProductId.Should().Be(productId);
        result.AccessStatus.Should().Be(ProductAccessStatus.Active);
        _mockUserProductRepository.Verify(r => r.AddAsync(It.IsAny<UserProduct>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUserProductRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSubscriptionId_SetsSubscriptionId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var accessEndDate = DateTime.UtcNow.AddMonths(1);
        var command = new GrantProductAccessCommand(
            userId,
            productId,
            AcquisitionType: ProductAcquisitionType.Subscription,
            AccessEndDate: accessEndDate,
            SubscriptionId: subscriptionId);

        _mockProductRepository
            .Setup(r => r.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProduct?)null);

        UserProduct? capturedUserProduct = null;
        _mockUserProductRepository
            .Setup(r => r.AddAsync(It.IsAny<UserProduct>(), It.IsAny<CancellationToken>()))
            .Callback<UserProduct, CancellationToken>((up, _) => capturedUserProduct = up)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUserProduct.Should().NotBeNull();
        capturedUserProduct!.SubscriptionId.Should().Be(subscriptionId);
        capturedUserProduct.AccessEndDate.Should().Be(accessEndDate);
        capturedUserProduct.AcquisitionType.Should().Be(ProductAcquisitionType.Subscription);
    }

    [Fact]
    public async Task Handle_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(ProductAcquisitionType.Purchase)]
    [InlineData(ProductAcquisitionType.Grant)]
    [InlineData(ProductAcquisitionType.Gift)]
    [InlineData(ProductAcquisitionType.Subscription)]
    [InlineData(ProductAcquisitionType.PromoCode)]
    public async Task Handle_WithVariousAcquisitionTypes_SetsTypeCorrectly(ProductAcquisitionType acquisitionType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new GrantProductAccessCommand(
            userId,
            productId,
            AcquisitionType: acquisitionType);

        _mockProductRepository
            .Setup(r => r.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProduct?)null);

        UserProduct? capturedUserProduct = null;
        _mockUserProductRepository
            .Setup(r => r.AddAsync(It.IsAny<UserProduct>(), It.IsAny<CancellationToken>()))
            .Callback<UserProduct, CancellationToken>((up, _) => capturedUserProduct = up)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUserProduct.Should().NotBeNull();
        capturedUserProduct!.AcquisitionType.Should().Be(acquisitionType);
    }

    [Fact]
    public async Task Handle_WhenUpdatingAccess_SetsUpdatedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new GrantProductAccessCommand(userId, productId);

        var existingAccess = CreateUserProduct(userId, productId, ProductAccessStatus.Revoked);
        var originalUpdatedAt = existingAccess.UpdatedAt;

        _mockProductRepository
            .Setup(r => r.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccess);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingAccess.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    private static UserProduct CreateUserProduct(
        Guid userId,
        Guid productId,
        ProductAccessStatus status = ProductAccessStatus.Active)
    {
        var up = new UserProduct
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            AccessStatus = status,
            AcquisitionType = ProductAcquisitionType.Grant,
            PricePaid = 0,
            Currency = "USD"
        };
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(up, DateTime.UtcNow.AddDays(-1));
        typeof(EntityBase).GetProperty(nameof(EntityBase.UpdatedAt))!.SetValue(up, DateTime.UtcNow.AddDays(-1));
        return up;
    }
}

/// <summary>
/// Unit tests for RevokeProductAccessCommandHandler
/// </summary>
public class RevokeProductAccessCommandHandlerTests
{
    private readonly Mock<IUserProductRepository> _mockUserProductRepository;
    private readonly RevokeProductAccessCommandHandler _handler;

    public RevokeProductAccessCommandHandlerTests()
    {
        _mockUserProductRepository = new Mock<IUserProductRepository>();
        _handler = new RevokeProductAccessCommandHandler(_mockUserProductRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenUserHasAccess_RevokesAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new RevokeProductAccessCommand(userId, productId);

        var existingAccess = CreateUserProduct(userId, productId, ProductAccessStatus.Active);

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccess);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(CqrsUnit.Value);
        existingAccess.AccessStatus.Should().Be(ProductAccessStatus.Revoked);
        _mockUserProductRepository.Verify(r => r.UpdateAsync(existingAccess, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserProductRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotHaveAccess_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new RevokeProductAccessCommand(userId, productId);

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProduct?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{userId}*{productId}*");
    }

    [Fact]
    public async Task Handle_WhenRevokingAccess_SetsUpdatedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new RevokeProductAccessCommand(userId, productId);

        var existingAccess = CreateUserProduct(userId, productId, ProductAccessStatus.Active);
        var originalUpdatedAt = existingAccess.UpdatedAt;

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccess);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingAccess.UpdatedAt.Should().BeAfter(originalUpdatedAt);
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
    public async Task Handle_WithAlreadyRevokedAccess_StillUpdatesAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new RevokeProductAccessCommand(userId, productId);

        var existingAccess = CreateUserProduct(userId, productId, ProductAccessStatus.Revoked);

        _mockUserProductRepository
            .Setup(r => r.GetByUserAndProductAsync(userId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccess);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(CqrsUnit.Value);
        _mockUserProductRepository.Verify(r => r.UpdateAsync(existingAccess, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UserProduct CreateUserProduct(
        Guid userId,
        Guid productId,
        ProductAccessStatus status = ProductAccessStatus.Active)
    {
        var up = new UserProduct
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            AccessStatus = status,
            AcquisitionType = ProductAcquisitionType.Grant,
            PricePaid = 0,
            Currency = "USD"
        };
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(up, DateTime.UtcNow.AddDays(-1));
        typeof(EntityBase).GetProperty(nameof(EntityBase.UpdatedAt))!.SetValue(up, DateTime.UtcNow.AddDays(-1));
        return up;
    }
}
