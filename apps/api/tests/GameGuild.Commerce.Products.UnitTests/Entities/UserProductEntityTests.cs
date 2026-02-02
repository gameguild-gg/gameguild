using FluentAssertions;
using GameGuild.Commerce.Products;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Entities;

/// <summary>
/// Unit tests for UserProduct entity
/// </summary>
public class UserProductEntityTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var userProduct = new UserProduct();

        // Assert
        userProduct.AccessStatus.Should().Be(ProductAccessStatus.Active);
        userProduct.Currency.Should().Be("USD");
        userProduct.CancelAtPeriodEnd.Should().BeFalse();
    }

    #endregion

    #region HasActiveAccess Tests

    [Fact]
    public void HasActiveAccess_WhenActiveWithNoDateConstraints_ShouldReturnTrue()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Active,
            AccessStartDate = null,
            AccessEndDate = null
        };

        // Act
        var result = userProduct.HasActiveAccess();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasActiveAccess_WhenStatusRevoked_ShouldReturnFalse()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Revoked
        };

        // Act
        var result = userProduct.HasActiveAccess();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasActiveAccess_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Active,
            AccessStartDate = DateTime.UtcNow.AddDays(-10),
            AccessEndDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = userProduct.HasActiveAccess();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasActiveAccess_WhenNotYetStarted_ShouldReturnFalse()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Active,
            AccessStartDate = DateTime.UtcNow.AddDays(1),
            AccessEndDate = null
        };

        // Act
        var result = userProduct.HasActiveAccess();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasActiveAccess_WhenWithinAccessPeriod_ShouldReturnTrue()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Active,
            AccessStartDate = DateTime.UtcNow.AddDays(-1),
            AccessEndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = userProduct.HasActiveAccess();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(ProductAccessStatus.Pending)]
    [InlineData(ProductAccessStatus.Revoked)]
    [InlineData(ProductAccessStatus.Expired)]
    [InlineData(ProductAccessStatus.Suspended)]
    public void HasActiveAccess_WithNonActiveStatuses_ShouldReturnFalse(ProductAccessStatus status)
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = status,
            AccessStartDate = DateTime.UtcNow.AddDays(-1),
            AccessEndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = userProduct.HasActiveAccess();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GrantAccess Tests

    [Fact]
    public void GrantAccess_ShouldSetActiveStatusAndStartDate()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Pending
        };

        // Act
        userProduct.GrantAccess();

        // Assert
        userProduct.AccessStatus.Should().Be(ProductAccessStatus.Active);
        userProduct.AccessStartDate.Should().NotBeNull();
        userProduct.AccessStartDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GrantAccess_WithEndDate_ShouldSetAccessEndDate()
    {
        // Arrange
        var userProduct = new UserProduct();
        var endDate = DateTime.UtcNow.AddMonths(1);

        // Act
        userProduct.GrantAccess(endDate: endDate);

        // Assert
        userProduct.AccessEndDate.Should().Be(endDate);
    }

    [Fact]
    public void GrantAccess_WithPricePaid_ShouldSetPricePaid()
    {
        // Arrange
        var userProduct = new UserProduct();

        // Act
        userProduct.GrantAccess(pricePaid: 99.99m);

        // Assert
        userProduct.PricePaid.Should().Be(99.99m);
    }

    [Fact]
    public void GrantAccess_WithCurrency_ShouldSetCurrency()
    {
        // Arrange
        var userProduct = new UserProduct();

        // Act
        userProduct.GrantAccess(currency: "EUR");

        // Assert
        userProduct.Currency.Should().Be("EUR");
    }

    [Fact]
    public void GrantAccess_WithAcquisitionType_ShouldSetAcquisitionType()
    {
        // Arrange
        var userProduct = new UserProduct();

        // Act
        userProduct.GrantAccess(acquisitionType: ProductAcquisitionType.Gift);

        // Assert
        userProduct.AcquisitionType.Should().Be(ProductAcquisitionType.Gift);
    }

    [Fact]
    public void GrantAccess_ShouldNotOverwriteExistingStartDate()
    {
        // Arrange
        var originalStartDate = DateTime.UtcNow.AddDays(-5);
        var userProduct = new UserProduct
        {
            AccessStartDate = originalStartDate
        };

        // Act
        userProduct.GrantAccess();

        // Assert
        userProduct.AccessStartDate.Should().Be(originalStartDate);
    }

    #endregion

    #region RevokeAccess Tests

    [Fact]
    public void RevokeAccess_ShouldSetRevokedStatusAndEndDate()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Active
        };

        // Act
        userProduct.RevokeAccess();

        // Assert
        userProduct.AccessStatus.Should().Be(ProductAccessStatus.Revoked);
        userProduct.AccessEndDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RevokeAccess_WithReason_ShouldSetRevocationReason()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Active
        };

        // Act
        userProduct.RevokeAccess("Terms violation");

        // Assert
        userProduct.RevocationReason.Should().Be("Terms violation");
    }

    [Fact]
    public void RevokeAccess_WithActiveSubscription_ShouldCancelSubscription()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Active,
            SubscriptionStatus = EntitlementSubscriptionStatus.Active
        };

        // Act
        userProduct.RevokeAccess("Requested cancellation");

        // Assert
        userProduct.SubscriptionStatus.Should().Be(EntitlementSubscriptionStatus.Cancelled);
    }

    [Fact]
    public void RevokeAccess_WithoutSubscription_ShouldNotAffectSubscriptionStatus()
    {
        // Arrange
        var userProduct = new UserProduct
        {
            AccessStatus = ProductAccessStatus.Active,
            SubscriptionStatus = null
        };

        // Act
        userProduct.RevokeAccess();

        // Assert
        userProduct.SubscriptionStatus.Should().BeNull();
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateUserProduct()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var userProduct = UserProduct.Create(
            userId: userId,
            productId: productId,
            acquisitionType: ProductAcquisitionType.Purchase,
            pricePaid: 49.99m,
            currency: "USD",
            expiresAt: DateTime.UtcNow.AddYears(1),
            tenantId: tenantId);

        // Assert
        userProduct.Id.Should().NotBe(Guid.Empty);
        userProduct.UserId.Should().Be(userId);
        userProduct.ProductId.Should().Be(productId);
        userProduct.AcquisitionType.Should().Be(ProductAcquisitionType.Purchase);
        userProduct.AccessStatus.Should().Be(ProductAccessStatus.Active);
        userProduct.PricePaid.Should().Be(49.99m);
        userProduct.Currency.Should().Be("USD");
        userProduct.AccessStartDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        userProduct.AccessEndDate.Should().BeCloseTo(DateTime.UtcNow.AddYears(1), TimeSpan.FromSeconds(1));
        userProduct.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithDefaultValues_ShouldUseDefaults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var userProduct = UserProduct.Create(
            userId: userId,
            productId: productId,
            acquisitionType: ProductAcquisitionType.Free);

        // Assert
        userProduct.PricePaid.Should().Be(0);
        userProduct.Currency.Should().Be("USD");
        userProduct.AccessEndDate.Should().BeNull();
        userProduct.TenantId.Should().BeNull();
    }

    [Theory]
    [InlineData(ProductAcquisitionType.Purchase)]
    [InlineData(ProductAcquisitionType.Subscription)]
    [InlineData(ProductAcquisitionType.Gift)]
    [InlineData(ProductAcquisitionType.Free)]
    [InlineData(ProductAcquisitionType.Trial)]
    [InlineData(ProductAcquisitionType.PromoCode)]
    [InlineData(ProductAcquisitionType.Bundle)]
    public void Create_WithAllAcquisitionTypes_ShouldSucceed(ProductAcquisitionType acquisitionType)
    {
        // Act
        var userProduct = UserProduct.Create(
            userId: Guid.NewGuid(),
            productId: Guid.NewGuid(),
            acquisitionType: acquisitionType);

        // Assert
        userProduct.AcquisitionType.Should().Be(acquisitionType);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void SubscriptionId_WhenSet_ShouldRetainValue()
    {
        // Arrange
        var userProduct = new UserProduct();
        var subscriptionId = Guid.NewGuid();

        // Act
        userProduct.SubscriptionId = subscriptionId;

        // Assert
        userProduct.SubscriptionId.Should().Be(subscriptionId);
    }

    [Fact]
    public void GiftedByUserId_WhenSet_ShouldRetainValue()
    {
        // Arrange
        var userProduct = new UserProduct();
        var gifterId = Guid.NewGuid();

        // Act
        userProduct.GiftedByUserId = gifterId;

        // Assert
        userProduct.GiftedByUserId.Should().Be(gifterId);
    }

    [Fact]
    public void OrderId_WhenSet_ShouldRetainValue()
    {
        // Arrange
        var userProduct = new UserProduct();
        var orderId = Guid.NewGuid();

        // Act
        userProduct.OrderId = orderId;

        // Assert
        userProduct.OrderId.Should().Be(orderId);
    }

    [Theory]
    [InlineData(EntitlementSubscriptionStatus.Active)]
    [InlineData(EntitlementSubscriptionStatus.Cancelled)]
    [InlineData(EntitlementSubscriptionStatus.PastDue)]
    [InlineData(EntitlementSubscriptionStatus.Paused)]
    public void SubscriptionStatus_ShouldAcceptAllValues(EntitlementSubscriptionStatus status)
    {
        // Arrange
        var userProduct = new UserProduct();

        // Act
        userProduct.SubscriptionStatus = status;

        // Assert
        userProduct.SubscriptionStatus.Should().Be(status);
    }

    #endregion
}

/// <summary>
/// Unit tests for ProductAccessStatus enum
/// </summary>
public class ProductAccessStatusEnumTests
{
    [Theory]
    [InlineData(ProductAccessStatus.Active, 0)]
    [InlineData(ProductAccessStatus.Expired, 1)]
    [InlineData(ProductAccessStatus.Revoked, 2)]
    [InlineData(ProductAccessStatus.Suspended, 3)]
    [InlineData(ProductAccessStatus.Pending, 4)]
    [InlineData(ProductAccessStatus.Cancelled, 5)]
    public void ProductAccessStatus_ShouldHaveExpectedIntValues(ProductAccessStatus status, int expectedValue)
    {
        // Assert
        ((int)status).Should().Be(expectedValue);
    }

    [Fact]
    public void ProductAccessStatus_ShouldHaveCorrectCount()
    {
        // Assert
        Enum.GetValues<ProductAccessStatus>().Should().HaveCount(6);
    }
}

/// <summary>
/// Unit tests for EntitlementSubscriptionStatus enum
/// </summary>
public class EntitlementSubscriptionStatusEnumTests
{
    [Theory]
    [InlineData(EntitlementSubscriptionStatus.Active)]
    [InlineData(EntitlementSubscriptionStatus.Cancelled)]
    [InlineData(EntitlementSubscriptionStatus.PastDue)]
    [InlineData(EntitlementSubscriptionStatus.Paused)]
    public void EntitlementSubscriptionStatus_ShouldBeValid(EntitlementSubscriptionStatus status)
    {
        // Assert
        Enum.IsDefined(typeof(EntitlementSubscriptionStatus), status).Should().BeTrue();
    }
}
