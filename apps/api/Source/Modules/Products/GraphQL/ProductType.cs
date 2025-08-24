using System.Security.Claims;
using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;
using ProductEntity = GameGuild.Modules.Products.Product;


namespace GameGuild.Modules.Products;

/// <summary>
/// GraphQL type definition for Product entity with DAC permission integration
/// </summary>
public class ProductType : ObjectType<Product> {
  protected override void Configure(IObjectTypeDescriptor<ProductEntity> descriptor) {
    descriptor.Name("Product");
    descriptor.Description("Represents a product in the CMS system with full EntityBase support and DAC permissions.");

    // Base Entity Properties
    descriptor.Field(p => p.Id)
              .Type<NonNullType<UuidType>>()
              .Description("The unique identifier for the product (UUID).");

    descriptor.Field(p => p.Version).Description("Version control for optimistic concurrency.");

    descriptor.Field(p => p.CreatedAt)
              .Type<NonNullType<DateTimeType>>()
              .Description("The date and time when the product was created.");

    descriptor.Field(p => p.UpdatedAt)
              .Type<DateTimeType>()
              .Description("The date and time when the product was last updated.");

    descriptor.Field(p => p.DeletedAt)
              .Type<DateTimeType>()
              .Description("The date and time when the product was soft deleted (null if not deleted).");

    descriptor.Field(p => p.IsDeleted)
              .Type<NonNullType<BooleanType>>()
              .Description("Indicates whether the product has been soft deleted.");

    // Product-specific Properties
    descriptor.Field(p => p.Title).Type<NonNullType<StringType>>().Description("The title of the product.");

    descriptor.Field(p => p.ShortDescription).Type<StringType>().Description("Short description of the product.");

    descriptor.Field(p => p.Description).Type<StringType>().Description("Detailed description of the product.");

    descriptor.Field(p => p.Type).Type<NonNullType<EnumType<ProductType>>>().Description("The type of the product.");

    descriptor.Field(p => p.Status)
              .Type<NonNullType<EnumType<ContentStatus>>>()
              .Description("The publication status of the product.");

    descriptor.Field(p => p.Visibility)
              .Type<NonNullType<EnumType<AccessLevel>>>()
              .Description("The access level of the product.");

    descriptor.Field(p => p.IsBundle)
              .Type<NonNullType<BooleanType>>()
              .Description("Indicates whether this product is a bundle containing other products.");

    descriptor.Field(p => p.Name)
              .Type<NonNullType<StringType>>()
              .Description("The name of the product (product-specific field).");

    descriptor.Field(p => p.Creator)
              .Type<ObjectType<User>>()
              .Description("The user who created this product.");

    descriptor.Field(p => p.Tenant)
              .Type<ObjectType<Tenant>>()
              .Description("The tenant this product belongs to.");

    // Related entities
    descriptor.Field(p => p.ProductPricings)
              .Type<ListType<ProductPricingType>>()
              .Description("Pricing information for this product.");

    descriptor.Field(p => p.UserProducts)
              .Type<ListType<UserProductType>>()
              .Description("User access records for this product.");

    descriptor.Field(p => p.PromoCodes)
              .Type<ListType<PromoCodeType>>()
              .Description("Promotional codes associated with this product.");

    // Computed fields based on DAC permissions
    descriptor.Field("canEdit")
              .Type<BooleanType>()
              .Description("Indicates if the current user can edit this product")
              .Resolve(async context => {
                var product = context.Parent<ProductEntity>();
                var user = context.GetUser();
                var userId = GetUserId(user);

                if (userId == null) return false;

                var permissionService = context.Service<IPermissionService>();

                // Hierarchical permission check: Resource → Content-Type → Tenant
                try {
                  // 1. Check resource-level permission
                  var hasResourcePermission =
                    await permissionService.HasResourcePermissionAsync<ProductPermission, ProductEntity>(
                      userId.Value,
                      product.Tenant?.Id,
                      product.Id,
                      PermissionType.Edit
                    );

                  if (hasResourcePermission) return true;
                }
                catch {
                  // Continue to fallbacks if resource checking fails
                }

                // 2. Check content-type permission
                var hasContentTypePermission =
                  await permissionService.HasContentTypePermissionAsync(
                    userId.Value,
                    product.Tenant?.Id,
                    "Product",
                    PermissionType.Edit
                  );

                if (hasContentTypePermission) return true;

                // 3. Check tenant permission
                var hasTenantPermission =
                  await permissionService.HasTenantPermissionAsync(userId.Value, product.Tenant?.Id, PermissionType.Edit);

                return hasTenantPermission;
              }
              );

    descriptor.Field("canDelete")
              .Type<BooleanType>()
              .Description("Indicates if the current user can delete this product")
              .Resolve(async context => {
                var product = context.Parent<ProductEntity>();
                var user = context.GetUser();
                var userId = GetUserId(user);

                if (userId == null) return false;

                var permissionService = context.Service<IPermissionService>();

                try {
                  var hasResourcePermission =
                    await permissionService.HasResourcePermissionAsync<ProductPermission, ProductEntity>(
                      userId.Value,
                      product.Tenant?.Id,
                      product.Id,
                      PermissionType.Delete
                    );

                  if (hasResourcePermission) return true;
                }
                catch { }

                var hasContentTypePermission =
                  await permissionService.HasContentTypePermissionAsync(
                    userId.Value,
                    product.Tenant?.Id,
                    "Product",
                    PermissionType.Delete
                  );

                if (hasContentTypePermission) return true;

                var hasTenantPermission =
                  await permissionService.HasTenantPermissionAsync(userId.Value, product.Tenant?.Id, PermissionType.Delete);

                return hasTenantPermission;
              }
              );

    descriptor.Field("canPublish")
              .Type<BooleanType>()
              .Description("Indicates if the current user can publish this product")
              .Resolve(async context => {
                var product = context.Parent<ProductEntity>();
                var user = context.GetUser();
                var userId = GetUserId(user);

                if (userId == null) return false;

                var permissionService = context.Service<IPermissionService>();

                try {
                  var hasResourcePermission =
                    await permissionService.HasResourcePermissionAsync<ProductPermission, ProductEntity>(
                      userId.Value,
                      product.Tenant?.Id,
                      product.Id,
                      PermissionType.Publish
                    );

                  if (hasResourcePermission) return true;
                }
                catch { }

                var hasContentTypePermission =
                  await permissionService.HasContentTypePermissionAsync(
                    userId.Value,
                    product.Tenant?.Id,
                    "Product",
                    PermissionType.Publish
                  );

                if (hasContentTypePermission) return true;

                var hasTenantPermission =
                  await permissionService.HasTenantPermissionAsync(userId.Value, product.Tenant?.Id, PermissionType.Publish);

                return hasTenantPermission;
              }
              );

    descriptor.Field("hasAccess")
              .Type<BooleanType>()
              .Description("Indicates if the current user has access to this product")
              .Resolve(async context => {
                var product = context.Parent<ProductEntity>();
                var user = context.GetUser();
                var userId = GetUserId(user);

                if (userId == null) return false;

                var productService = context.Service<IProductService>();

                return await productService.HasUserAccessAsync(userId.Value, product.Id);
              }
              );

    descriptor.Field("currentPricing")
              .Type<ProductPricingType>()
              .Description("The current active pricing for this product")
              .Resolve(async context => {
                var product = context.Parent<ProductEntity>();
                var productService = context.Service<IProductService>();

                return await productService.GetCurrentPricingAsync(product.Id);
              }
              );

    descriptor.Field("bundleItems")
              .Type<ListType<ProductType>>()
              .Description("Items included in this bundle (only applicable if IsBundle is true)")
              .Resolve(async context => {
                var product = context.Parent<ProductEntity>();

                if (!product.IsBundle) return new List<ProductEntity>();

                var productService = context.Service<IProductService>();

                return await productService.GetBundleItemsAsync(product.Id);
              }
              );
  }

  private static Guid? GetUserId(ClaimsPrincipal? user) {
    if (user?.Identity?.IsAuthenticated != true) return null;

    var userIdClaim = user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier);

    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)) return userId;

    return null;
  }
}
