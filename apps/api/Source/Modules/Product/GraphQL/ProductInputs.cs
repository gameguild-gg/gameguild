using System.Diagnostics.CodeAnalysis;
using GameGuild.Common.Enums;
using GameGuild.Common.Entities;
using PromoCodeTypeEnum = GameGuild.Common.Enums.PromoCodeType;


namespace GameGuild.Modules.Product.GraphQL;

public class CreateProductInput {
  private string _name;

  private string? _shortDescription;

  private Common.Enums.ProductType _type;

  private bool _isBundle = false;

  private Guid? _tenantId;

  public required string Name {
    get => _name;
    [MemberNotNull(nameof(_name))] set => _name = value;
  }

  public string? ShortDescription {
    get => _shortDescription;
    set => _shortDescription = value;
  }

  public required GameGuild.Common.Enums.ProductType Type {
    get => _type;
    set => _type = value;
  }

  public bool IsBundle {
    get => _isBundle;
    set => _isBundle = value;
  }

  public Guid? TenantId {
    get => _tenantId;
    set => _tenantId = value;
  }
}

public class UpdateProductInput {
  private Guid _id;

  private string? _name;

  private string? _shortDescription;

  private string? _description;

  private Common.Enums.ProductType? _type;

  private bool? _isBundle;

  private ContentStatus? _status;

  private AccessLevel? _visibility;

  public required Guid Id {
    get => _id;
    set => _id = value;
  }

  public string? Name {
    get => _name;
    set => _name = value;
  }

  public string? ShortDescription {
    get => _shortDescription;
    set => _shortDescription = value;
  }

  public string? Description {
    get => _description;
    set => _description = value;
  }

  public GameGuild.Common.Enums.ProductType? Type {
    get => _type;
    set => _type = value;
  }

  public bool? IsBundle {
    get => _isBundle;
    set => _isBundle = value;
  }

  public ContentStatus? Status {
    get => _status;
    set => _status = value;
  }

  public Common.Entities.AccessLevel? Visibility {
    get => _visibility;
    set => _visibility = value;
  }
}

public class BundleManagementInput {
  private Guid _bundleId;

  private Guid _productId;

  public Guid BundleId {
    get => _bundleId;
    set => _bundleId = value;
  }

  public Guid ProductId {
    get => _productId;
    set => _productId = value;
  }
}

public class SetProductPricingInput {
  private Guid _productId;

  private decimal _basePrice;

  private string _currency = "USD";

  public required Guid ProductId {
    get => _productId;
    set => _productId = value;
  }

  public required decimal BasePrice {
    get => _basePrice;
    set => _basePrice = value;
  }

  public required string Currency {
    get => _currency;
    [MemberNotNull(nameof(_currency))] set => _currency = value;
  }
}

public class UpdateProductPricingInput {
  private Guid _pricingId;

  private decimal? _basePrice;

  private string? _currency;

  public required Guid PricingId {
    get => _pricingId;
    set => _pricingId = value;
  }

  public decimal? BasePrice {
    get => _basePrice;
    set => _basePrice = value;
  }

  public string? Currency {
    get => _currency;
    set => _currency = value;
  }
}

public class GrantProductAccessInput {
  private Guid _productId;

  private Guid _userId;

  private ProductAcquisitionType _acquisitionType;

  private decimal _purchasePrice;

  private string? _currency;

  private DateTime? _expiresAt;

  public required Guid ProductId {
    get => _productId;
    set => _productId = value;
  }

  public required Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  public required ProductAcquisitionType AcquisitionType {
    get => _acquisitionType;
    set => _acquisitionType = value;
  }

  public required decimal PurchasePrice {
    get => _purchasePrice;
    set => _purchasePrice = value;
  }

  public string? Currency {
    get => _currency;
    set => _currency = value;
  }

  public DateTime? ExpiresAt {
    get => _expiresAt;
    set => _expiresAt = value;
  }
}

public class CreatePromoCodeInput {
  private Guid _productId;

  private string _code;

  private decimal _discountPercentage;

  private DateTime? _expiryDate;

  private PromoCodeTypeEnum _discountType;

  private DateTime? _validFrom;

  private DateTime? _validUntil;

  private int? _maxUses;

  private decimal _discountValue;

  public required Guid ProductId {
    get => _productId;
    set => _productId = value;
  }

  public required string Code {
    get => _code;
    [MemberNotNull(nameof(_code))] set => _code = value;
  }

  public required decimal DiscountPercentage {
    get => _discountPercentage;
    set => _discountPercentage = value;
  }

  public DateTime? ExpiryDate {
    get => _expiryDate;
    set => _expiryDate = value;
  }

  public required PromoCodeTypeEnum DiscountType {
    get => _discountType;
    set => _discountType = value;
  }

  public DateTime? ValidFrom {
    get => _validFrom;
    set => _validFrom = value;
  }

  public DateTime? ValidUntil {
    get => _validUntil;
    set => _validUntil = value;
  }

  public int? MaxUses {
    get => _maxUses;
    set => _maxUses = value;
  }

  public required decimal DiscountValue {
    get => _discountValue;
    set => _discountValue = value;
  }
}

public class UpdatePromoCodeInput {
  private Guid _id;

  private string? _code;

  private decimal? _discountPercentage;

  private DateTime? _expiryDate;

  private PromoCodeTypeEnum? _discountType;

  private DateTime? _validFrom;

  private DateTime? _validUntil;

  private int? _maxUses;

  private decimal? _discountValue;

  public required Guid Id {
    get => _id;
    set => _id = value;
  }

  public string? Code {
    get => _code;
    set => _code = value;
  }

  public decimal? DiscountPercentage {
    get => _discountPercentage;
    set => _discountPercentage = value;
  }

  public DateTime? ExpiryDate {
    get => _expiryDate;
    set => _expiryDate = value;
  }

  public PromoCodeTypeEnum? DiscountType {
    get => _discountType;
    set => _discountType = value;
  }

  public DateTime? ValidFrom {
    get => _validFrom;
    set => _validFrom = value;
  }

  public DateTime? ValidUntil {
    get => _validUntil;
    set => _validUntil = value;
  }

  public int? MaxUses {
    get => _maxUses;
    set => _maxUses = value;
  }

  public decimal? DiscountValue {
    get => _discountValue;
    set => _discountValue = value;
  }
}
