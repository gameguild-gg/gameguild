namespace GameGuild.Identity.Authorization;

/// <summary>
///     Central registry providing convenient access to all permission key constants.
///     Use these constants in attributes where compile-time constants are required.
/// </summary>
/// <remarks>
///     <para>
///         This class provides backward-compatible access to permission keys.
///         For runtime permission checks, prefer using the strongly-typed permission classes
///         (e.g., <see cref="UsersPermission.Read"/>) which provide IntelliSense and refactoring safety.
///     </para>
///     <para>
///         For attributes, use either this class or the nested Keys class in each permission type:
///         <code>
///         // Option 1: Using this facade
///         [RequirePermission(Permissions.UsersRead)]
///         
///         // Option 2: Using the nested Keys class (preferred for discoverability)
///         [RequirePermission(UsersPermission.Keys.Read)]
///         </code>
///     </para>
/// </remarks>
public static class Permissions
{
    // ========================
    // ADMIN PERMISSIONS
    // ========================

    /// <summary>Full admin access (wildcard)</summary>
    public const string AdminWildcard = AdminPermission.Keys.Wildcard;

    /// <summary>Admin permission</summary>
    public const string Admin = AdminPermission.Keys.Admin;

    /// <summary>Tenant admin permission</summary>
    public const string TenantAdmin = AdminPermission.Keys.TenantAdmin;

    // ========================
    // USER PERMISSIONS - CRUD Operations
    // ========================

    /// <summary>Read user data</summary>
    public const string UsersRead = UsersPermission.Keys.Read;

    /// <summary>Create new users</summary>
    public const string UsersCreate = UsersPermission.Keys.Create;

    /// <summary>Update existing users</summary>
    public const string UsersUpdate = UsersPermission.Keys.Update;

    /// <summary>Soft-delete users</summary>
    public const string UsersDelete = UsersPermission.Keys.Delete;

    /// <summary>Administrative operations on users</summary>
    public const string UsersAdmin = UsersPermission.Keys.Admin;

    /// <summary>Permanently delete users (dangerous)</summary>
    public const string UsersPurge = UsersPermission.Keys.Purge;

    // ========================
    // USER PERMISSIONS - Self Operations
    // ========================

    /// <summary>Edit own profile</summary>
    public const string UsersEditSelf = UsersPermission.Keys.EditSelf;

    /// <summary>Delete own account</summary>
    public const string UsersDeleteSelf = UsersPermission.Keys.DeleteSelf;

    /// <summary>Read own data</summary>
    public const string UsersReadSelf = UsersPermission.Keys.ReadSelf;

    /// <summary>Manage any user</summary>
    public const string UsersManage = UsersPermission.Keys.Manage;

    // ========================
    // CONTENT PERMISSIONS
    // ========================

    /// <summary>Read content</summary>
    public const string ContentRead = ContentPermission.Keys.Read;

    /// <summary>Write/edit content</summary>
    public const string ContentWrite = ContentPermission.Keys.Write;

    /// <summary>Admin access to content</summary>
    public const string ContentAdmin = ContentPermission.Keys.Admin;

    // ========================
    // PROJECT PERMISSIONS
    // ========================

    /// <summary>Read projects</summary>
    public const string ProjectRead = ProjectPermission.Keys.Read;

    /// <summary>Write/edit projects</summary>
    public const string ProjectWrite = ProjectPermission.Keys.Write;

    /// <summary>Admin access to projects</summary>
    public const string ProjectAdmin = ProjectPermission.Keys.Admin;

    // ========================
    // COURSE PERMISSIONS
    // ========================

    /// <summary>Read courses</summary>
    public const string CourseRead = CoursePermission.Keys.Read;

    /// <summary>Manage courses</summary>
    public const string CourseManage = CoursePermission.Keys.Manage;

    // ========================
    // PRODUCTS PERMISSIONS
    // ========================

    /// <summary>Read product data</summary>
    public const string ProductsRead = ProductsPermission.Keys.Read;

    /// <summary>Create new products</summary>
    public const string ProductsCreate = ProductsPermission.Keys.Create;

    /// <summary>Update existing products</summary>
    public const string ProductsUpdate = ProductsPermission.Keys.Update;

    /// <summary>Delete products</summary>
    public const string ProductsDelete = ProductsPermission.Keys.Delete;

    /// <summary>Full management access to products</summary>
    public const string ProductsManage = ProductsPermission.Keys.Manage;

    /// <summary>Manage product pricing</summary>
    public const string ProductsPricingManage = ProductsPermission.Keys.PricingManage;

    // ========================
    // PROMO CODES PERMISSIONS
    // ========================

    /// <summary>Read promo codes</summary>
    public const string PromoCodesRead = PromoCodesPermission.Keys.Read;

    /// <summary>Create promo codes</summary>
    public const string PromoCodesCreate = PromoCodesPermission.Keys.Create;

    /// <summary>Update promo codes</summary>
    public const string PromoCodesUpdate = PromoCodesPermission.Keys.Update;

    /// <summary>Delete promo codes</summary>
    public const string PromoCodesDelete = PromoCodesPermission.Keys.Delete;

    /// <summary>Full management access to promo codes</summary>
    public const string PromoCodesManage = PromoCodesPermission.Keys.Manage;

    // ========================
    // ORDERS PERMISSIONS
    // ========================

    /// <summary>Read orders (own orders)</summary>
    public const string OrdersRead = OrdersPermission.Keys.Read;

    /// <summary>Read all orders (admin)</summary>
    public const string OrdersReadAll = OrdersPermission.Keys.ReadAll;

    /// <summary>Create orders</summary>
    public const string OrdersCreate = OrdersPermission.Keys.Create;

    /// <summary>Process refunds</summary>
    public const string OrdersRefund = OrdersPermission.Keys.Refund;

    /// <summary>Full management access to orders</summary>
    public const string OrdersManage = OrdersPermission.Keys.Manage;

    // ========================
    // ENTITLEMENTS PERMISSIONS
    // ========================

    /// <summary>View own entitlements</summary>
    public const string EntitlementsReadSelf = EntitlementsPermission.Keys.ReadSelf;

    /// <summary>View all entitlements (admin)</summary>
    public const string EntitlementsReadAll = EntitlementsPermission.Keys.ReadAll;

    /// <summary>Grant entitlements</summary>
    public const string EntitlementsGrant = EntitlementsPermission.Keys.Grant;

    /// <summary>Revoke entitlements</summary>
    public const string EntitlementsRevoke = EntitlementsPermission.Keys.Revoke;

    /// <summary>Full management access to entitlements</summary>
    public const string EntitlementsManage = EntitlementsPermission.Keys.Manage;
}
