namespace GameGuild.Authorization;

/// <summary>
///     Central registry of all permission keys used in the system.
///     Use these constants instead of magic strings when checking permissions.
///     Format: resource:action[:scope] (colon-separated hierarchy)
/// </summary>
public static class Permissions
{
    // ========================
    // ADMIN PERMISSIONS
    // ========================

    /// <summary>Full admin access (wildcard)</summary>
    public const string AdminWildcard = "admin:*";

    /// <summary>Admin permission</summary>
    public const string Admin = "admin";

    /// <summary>Tenant admin permission</summary>
    public const string TenantAdmin = "tenant:admin";

    // ========================
    // USER PERMISSIONS - CRUD Operations
    // ========================

    /// <summary>Read user data</summary>
    public const string UsersRead = "users:read";

    /// <summary>Create new users</summary>
    public const string UsersCreate = "users:create";

    /// <summary>Update existing users</summary>
    public const string UsersUpdate = "users:update";

    /// <summary>Soft-delete users</summary>
    public const string UsersDelete = "users:delete";

    /// <summary>Administrative operations on users</summary>
    public const string UsersAdmin = "users:admin";

    /// <summary>Permanently delete users (dangerous)</summary>
    public const string UsersPurge = "users:purge";

    // ========================
    // USER PERMISSIONS - Self Operations
    // ========================

    /// <summary>Edit own profile</summary>
    public const string UsersEditSelf = "users:edit:self";

    /// <summary>Delete own account</summary>
    public const string UsersDeleteSelf = "users:delete:self";

    /// <summary>Read own data</summary>
    public const string UsersReadSelf = "users:read:self";

    /// <summary>Manage any user</summary>
    public const string UsersManage = "users:manage";

    // ========================
    // CONTENT PERMISSIONS
    // ========================

    /// <summary>Read content</summary>
    public const string ContentRead = "content:read";

    /// <summary>Write/edit content</summary>
    public const string ContentWrite = "content:write";

    /// <summary>Admin access to content</summary>
    public const string ContentAdmin = "content:admin";

    // ========================
    // PROJECT PERMISSIONS
    // ========================

    /// <summary>Read projects</summary>
    public const string ProjectRead = "project:read";

    /// <summary>Write/edit projects</summary>
    public const string ProjectWrite = "project:write";

    /// <summary>Admin access to projects</summary>
    public const string ProjectAdmin = "project:admin";

    // ========================
    // COURSE PERMISSIONS
    // ========================

    /// <summary>Read courses</summary>
    public const string CourseRead = "course:read";

    /// <summary>Manage courses</summary>
    public const string CourseManage = "course:manage";

    // ========================
    // PRODUCTS PERMISSIONS
    // ========================

    /// <summary>Read product data</summary>
    public const string ProductsRead = "products:read";

    /// <summary>Create new products</summary>
    public const string ProductsCreate = "products:create";

    /// <summary>Update existing products</summary>
    public const string ProductsUpdate = "products:update";

    /// <summary>Delete products</summary>
    public const string ProductsDelete = "products:delete";

    /// <summary>Full management access to products</summary>
    public const string ProductsManage = "products:manage";

    /// <summary>Manage product pricing</summary>
    public const string ProductsPricingManage = "products:pricing:manage";

    // ========================
    // PROMO CODE PERMISSIONS
    // ========================

    /// <summary>Read promo codes</summary>
    public const string PromoCodesRead = "promocodes:read";

    /// <summary>Create promo codes</summary>
    public const string PromoCodesCreate = "promocodes:create";

    /// <summary>Update promo codes</summary>
    public const string PromoCodesUpdate = "promocodes:update";

    /// <summary>Delete promo codes</summary>
    public const string PromoCodesDelete = "promocodes:delete";

    /// <summary>Full management access to promo codes</summary>
    public const string PromoCodesManage = "promocodes:manage";

    // ========================
    // ORDER PERMISSIONS
    // ========================

    /// <summary>Read orders (own orders)</summary>
    public const string OrdersRead = "orders:read";

    /// <summary>Read all orders (admin)</summary>
    public const string OrdersReadAll = "orders:read:all";

    /// <summary>Create orders</summary>
    public const string OrdersCreate = "orders:create";

    /// <summary>Process refunds</summary>
    public const string OrdersRefund = "orders:refund";

    /// <summary>Full management access to orders</summary>
    public const string OrdersManage = "orders:manage";

    // ========================
    // ENTITLEMENT PERMISSIONS
    // ========================

    /// <summary>View own entitlements</summary>
    public const string EntitlementsReadSelf = "entitlements:read:self";

    /// <summary>View all entitlements (admin)</summary>
    public const string EntitlementsReadAll = "entitlements:read:all";

    /// <summary>Grant entitlements</summary>
    public const string EntitlementsGrant = "entitlements:grant";

    /// <summary>Revoke entitlements</summary>
    public const string EntitlementsRevoke = "entitlements:revoke";

    /// <summary>Full management access to entitlements</summary>
    public const string EntitlementsManage = "entitlements:manage";
}
