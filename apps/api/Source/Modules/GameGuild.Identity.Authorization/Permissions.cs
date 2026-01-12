namespace GameGuild.Identity.Authorization;

/// <summary>
///     Central registry of all permission keys used in the system.
///     Use these constants instead of magic strings when checking permissions.
///     Format: resource:action[:scope] (colon-separated hierarchy)
/// </summary>
/// <remarks>
///     ⚠️ DEPRECATED: These string constants are being replaced with strongly-typed permission objects.
///     Use the typed permission classes (e.g., UsersPermission.Read) for compile-time safety.
///     See: docs/security/STRONGLY_TYPED_PERMISSIONS.md
/// </remarks>
[Obsolete("Use strongly-typed permission classes (e.g., UsersPermission.Read) for compile-time safety. " +
          "String constants will be removed in v2.0. See docs/security/STRONGLY_TYPED_PERMISSIONS.md")]
public static class Permissions
{
    // ========================
    // ADMIN PERMISSIONS
    // ========================

    /// <summary>Full admin access (wildcard)</summary>
    /// <remarks>⚠️ Use AdminPermission.Wildcard instead</remarks>
    public const string AdminWildcard = "admin:*";

    /// <summary>Admin permission</summary>
    /// <remarks>⚠️ Use AdminPermission.Admin instead</remarks>
    public const string Admin = "admin";

    /// <summary>Tenant admin permission</summary>
    /// <remarks>⚠️ Use AdminPermission.TenantAdmin instead</remarks>
    public const string TenantAdmin = "tenant:admin";

    // ========================
    // USER PERMISSIONS - CRUD Operations
    // ========================

    /// <summary>Read user data</summary>
    /// <remarks>⚠️ Use UsersPermission.Read instead</remarks>
    public const string UsersRead = "users:read";

    /// <summary>Create new users</summary>
    /// <remarks>⚠️ Use UsersPermission.Create instead</remarks>
    public const string UsersCreate = "users:create";

    /// <summary>Update existing users</summary>
    /// <remarks>⚠️ Use UsersPermission.Update instead</remarks>
    public const string UsersUpdate = "users:update";

    /// <summary>Soft-delete users</summary>
    /// <remarks>⚠️ Use UsersPermission.Delete instead</remarks>
    public const string UsersDelete = "users:delete";

    /// <summary>Administrative operations on users</summary>
    /// <remarks>⚠️ Use UsersPermission.Admin instead</remarks>
    public const string UsersAdmin = "users:admin";

    /// <summary>Permanently delete users (dangerous)</summary>
    /// <remarks>⚠️ Use UsersPermission.Purge instead</remarks>
    public const string UsersPurge = "users:purge";

    // ========================
    // USER PERMISSIONS - Self Operations
    // ========================

    /// <summary>Edit own profile</summary>
    /// <remarks>⚠️ Use UsersPermission.EditSelf instead</remarks>
    public const string UsersEditSelf = "users:edit:self";

    /// <summary>Delete own account</summary>
    /// <remarks>⚠️ Use UsersPermission.DeleteSelf instead</remarks>
    public const string UsersDeleteSelf = "users:delete:self";

    /// <summary>Read own data</summary>
    /// <remarks>⚠️ Use UsersPermission.ReadSelf instead</remarks>
    public const string UsersReadSelf = "users:read:self";

    /// <summary>Manage any user</summary>
    /// <remarks>⚠️ Use UsersPermission.Manage instead</remarks>
    public const string UsersManage = "users:manage";

    // ========================
    // CONTENT PERMISSIONS
    // ========================

    /// <summary>Read content</summary>
    /// <remarks>⚠️ Use ContentPermission.Read instead</remarks>
    public const string ContentRead = "content:read";

    /// <summary>Write/edit content</summary>
    /// <remarks>⚠️ Use ContentPermission.Write instead</remarks>
    public const string ContentWrite = "content:write";

    /// <summary>Admin access to content</summary>
    /// <remarks>⚠️ Use ContentPermission.Admin instead</remarks>
    public const string ContentAdmin = "content:admin";

    // ========================
    // PROJECT PERMISSIONS
    // ========================

    /// <summary>Read projects</summary>
    /// <remarks>⚠️ Use ProjectPermission.Read instead</remarks>
    public const string ProjectRead = "project:read";

    /// <summary>Write/edit projects</summary>
    /// <remarks>⚠️ Use ProjectPermission.Write instead</remarks>
    public const string ProjectWrite = "project:write";

    /// <summary>Admin access to projects</summary>
    /// <remarks>⚠️ Use ProjectPermission.Admin instead</remarks>
    public const string ProjectAdmin = "project:admin";

    // ========================
    // COURSE PERMISSIONS
    // ========================

    /// <summary>Read courses</summary>
    /// <remarks>⚠️ Use CoursePermission.Read instead</remarks>
    public const string CourseRead = "course:read";

    /// <summary>Manage courses</summary>
    /// <remarks>⚠️ Use CoursePermission.Manage instead</remarks>
    public const string CourseManage = "course:manage";

    // ========================
    // PRODUCTS PERMISSIONS
    // ========================

    /// <summary>Read product data</summary>
    /// <remarks>⚠️ Use ProductsPermission.Read instead</remarks>
    public const string ProductsRead = "products:read";

    /// <summary>Create new products</summary>
    /// <remarks>⚠️ Use ProductsPermission.Create instead</remarks>
    public const string ProductsCreate = "products:create";

    /// <summary>Update existing products</summary>
    /// <remarks>⚠️ Use ProductsPermission.Update instead</remarks>
    public const string ProductsUpdate = "products:update";

    /// <summary>Delete products</summary>
    /// <remarks>⚠️ Use ProductsPermission.Delete instead</remarks>
    public const string ProductsDelete = "products:delete";

    /// <summary>Full management access to products</summary>
    /// <remarks>⚠️ Use ProductsPermission.Manage instead</remarks>
    public const string ProductsManage = "products:manage";

    /// <summary>Manage product pricing</summary>
    /// <remarks>⚠️ Use ProductsPermission.PricingManage instead</remarks>
    public const string ProductsPricingManage = "products:pricing:manage";

    // ========================
    // PROMO CODE PERMISSIONS
    // ========================

    /// <summary>Read promo codes</summary>
    /// <remarks>⚠️ Use PromoCodesPermission.Read instead</remarks>
    public const string PromoCodesRead = "promocodes:read";

    /// <summary>Create promo codes</summary>
    /// <remarks>⚠️ Use PromoCodesPermission.Create instead</remarks>
    public const string PromoCodesCreate = "promocodes:create";

    /// <summary>Update promo codes</summary>
    /// <remarks>⚠️ Use PromoCodesPermission.Update instead</remarks>
    public const string PromoCodesUpdate = "promocodes:update";

    /// <summary>Delete promo codes</summary>
    /// <remarks>⚠️ Use PromoCodesPermission.Delete instead</remarks>
    public const string PromoCodesDelete = "promocodes:delete";

    /// <summary>Full management access to promo codes</summary>
    /// <remarks>⚠️ Use PromoCodesPermission.Manage instead</remarks>
    public const string PromoCodesManage = "promocodes:manage";

    // ========================
    // ORDER PERMISSIONS
    // ========================

    /// <summary>Read orders (own orders)</summary>
    /// <remarks>⚠️ Use OrdersPermission.Read instead</remarks>
    public const string OrdersRead = "orders:read";

    /// <summary>Read all orders (admin)</summary>
    /// <remarks>⚠️ Use OrdersPermission.ReadAll instead</remarks>
    public const string OrdersReadAll = "orders:read:all";

    /// <summary>Create orders</summary>
    /// <remarks>⚠️ Use OrdersPermission.Create instead</remarks>
    public const string OrdersCreate = "orders:create";

    /// <summary>Process refunds</summary>
    /// <remarks>⚠️ Use OrdersPermission.Refund instead</remarks>
    public const string OrdersRefund = "orders:refund";

    /// <summary>Full management access to orders</summary>
    /// <remarks>⚠️ Use OrdersPermission.Manage instead</remarks>
    public const string OrdersManage = "orders:manage";

    // ========================
    // ENTITLEMENT PERMISSIONS
    // ========================

    /// <summary>View own entitlements</summary>
    /// <remarks>⚠️ Use EntitlementsPermission.ReadSelf instead</remarks>
    public const string EntitlementsReadSelf = "entitlements:read:self";

    /// <summary>View all entitlements (admin)</summary>
    /// <remarks>⚠️ Use EntitlementsPermission.ReadAll instead</remarks>
    public const string EntitlementsReadAll = "entitlements:read:all";

    /// <summary>Grant entitlements</summary>
    /// <remarks>⚠️ Use EntitlementsPermission.Grant instead</remarks>
    public const string EntitlementsGrant = "entitlements:grant";

    /// <summary>Revoke entitlements</summary>
    /// <remarks>⚠️ Use EntitlementsPermission.Revoke instead</remarks>
    public const string EntitlementsRevoke = "entitlements:revoke";

    /// <summary>Full management access to entitlements</summary>
    /// <remarks>⚠️ Use EntitlementsPermission.Manage instead</remarks>
    public const string EntitlementsManage = "entitlements:manage";
}
