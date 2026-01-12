using GameGuild.Identity.Authorization.Models;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Strongly-typed permissions for admin operations.
///     Provides compile-time safety for permission checks.
/// </summary>
public sealed class AdminPermission : Permission
{
    private AdminPermission(string resource, string action, string? scope, string description)
        : base(resource, action, scope, description)
    {
    }

    /// <summary>Full admin access (wildcard)</summary>
    public static readonly AdminPermission Wildcard = new("admin", "*", null, "Full admin access (wildcard)");

    /// <summary>Admin permission</summary>
    public static readonly AdminPermission Admin = new("admin", "admin", null, "Admin permission");

    /// <summary>Tenant admin permission</summary>
    public static readonly AdminPermission TenantAdmin = new("tenant", "admin", null, "Tenant admin permission");
}

/// <summary>
///     Strongly-typed permissions for user operations.
///     Provides compile-time safety for permission checks.
/// </summary>
public sealed class UsersPermission : Permission
{
    private UsersPermission(string resource, string action, string? scope, string description)
        : base(resource, action, scope, description)
    {
    }

    // CRUD Operations
    /// <summary>Read user data</summary>
    public static readonly UsersPermission Read = new("users", "read", null, "Read user data");

    /// <summary>Create new users</summary>
    public static readonly UsersPermission Create = new("users", "create", null, "Create new users");

    /// <summary>Update existing users</summary>
    public static readonly UsersPermission Update = new("users", "update", null, "Update existing users");

    /// <summary>Soft-delete users</summary>
    public static readonly UsersPermission Delete = new("users", "delete", null, "Soft-delete users");

    /// <summary>Administrative operations on users</summary>
    public static readonly UsersPermission Admin = new("users", "admin", null, "Administrative operations on users");

    /// <summary>Permanently delete users (dangerous)</summary>
    public static readonly UsersPermission Purge = new("users", "purge", null, "Permanently delete users (dangerous)");

    // Self Operations
    /// <summary>Edit own profile</summary>
    public static readonly UsersPermission EditSelf = new("users", "edit", "self", "Edit own profile");

    /// <summary>Delete own account</summary>
    public static readonly UsersPermission DeleteSelf = new("users", "delete", "self", "Delete own account");

    /// <summary>Read own data</summary>
    public static readonly UsersPermission ReadSelf = new("users", "read", "self", "Read own data");

    /// <summary>Manage any user</summary>
    public static readonly UsersPermission Manage = new("users", "manage", null, "Manage any user");
}

/// <summary>
///     Strongly-typed permissions for content operations.
///     Provides compile-time safety for permission checks.
/// </summary>
public sealed class ContentPermission : Permission
{
    private ContentPermission(string resource, string action, string? scope, string description)
        : base(resource, action, scope, description)
    {
    }

    /// <summary>Read content</summary>
    public static readonly ContentPermission Read = new("content", "read", null, "Read content");

    /// <summary>Write/edit content</summary>
    public static readonly ContentPermission Write = new("content", "write", null, "Write/edit content");

    /// <summary>Admin access to content</summary>
    public static readonly ContentPermission Admin = new("content", "admin", null, "Admin access to content");
}

/// <summary>
///     Strongly-typed permissions for project operations.
///     Provides compile-time safety for permission checks.
/// </summary>
public sealed class ProjectPermission : Permission
{
    private ProjectPermission(string resource, string action, string? scope, string description)
        : base(resource, action, scope, description)
    {
    }

    /// <summary>Read projects</summary>
    public static readonly ProjectPermission Read = new("project", "read", null, "Read projects");

    /// <summary>Write/edit projects</summary>
    public static readonly ProjectPermission Write = new("project", "write", null, "Write/edit projects");

    /// <summary>Admin access to projects</summary>
    public static readonly ProjectPermission Admin = new("project", "admin", null, "Admin access to projects");
}

/// <summary>
///     Strongly-typed permissions for course operations.
///     Provides compile-time safety for permission checks.
/// </summary>
public sealed class CoursePermission : Permission
{
    private CoursePermission(string resource, string action, string? scope, string description)
        : base(resource, action, scope, description)
    {
    }

    /// <summary>Read courses</summary>
    public static readonly CoursePermission Read = new("course", "read", null, "Read courses");

    /// <summary>Manage courses</summary>
    public static readonly CoursePermission Manage = new("course", "manage", null, "Manage courses");
}

/// <summary>
///     Strongly-typed permissions for product operations.
///     Provides compile-time safety for permission checks.
/// </summary>
public sealed class ProductsPermission : Permission
{
    private ProductsPermission(string resource, string action, string? scope, string description)
        : base(resource, action, scope, description)
    {
    }

    /// <summary>Read product data</summary>
    public static readonly ProductsPermission Read = new("products", "read", null, "Read product data");

    /// <summary>Create new products</summary>
    public static readonly ProductsPermission Create = new("products", "create", null, "Create new products");

    /// <summary>Update existing products</summary>
    public static readonly ProductsPermission Update = new("products", "update", null, "Update existing products");

    /// <summary>Delete products</summary>
    public static readonly ProductsPermission Delete = new("products", "delete", null, "Delete products");

    /// <summary>Full management access to products</summary>
    public static readonly ProductsPermission Manage = new("products", "manage", null, "Full management access to products");

    /// <summary>Manage product pricing</summary>
    public static readonly ProductsPermission PricingManage = new("products", "pricing", "manage", "Manage product pricing");
}

/// <summary>
///     Strongly-typed permissions for promo code operations.
///     Provides compile-time safety for permission checks.
/// </summary>
public sealed class PromoCodesPermission : Permission
{
    private PromoCodesPermission(string resource, string action, string? scope, string description)
        : base(resource, action, scope, description)
    {
    }

    /// <summary>Read promo codes</summary>
    public static readonly PromoCodesPermission Read = new("promocodes", "read", null, "Read promo codes");

    /// <summary>Create promo codes</summary>
    public static readonly PromoCodesPermission Create = new("promocodes", "create", null, "Create promo codes");

    /// <summary>Update promo codes</summary>
    public static readonly PromoCodesPermission Update = new("promocodes", "update", null, "Update promo codes");

    /// <summary>Delete promo codes</summary>
    public static readonly PromoCodesPermission Delete = new("promocodes", "delete", null, "Delete promo codes");

    /// <summary>Full management access to promo codes</summary>
    public static readonly PromoCodesPermission Manage = new("promocodes", "manage", null, "Full management access to promo codes");
}

/// <summary>
///     Strongly-typed permissions for order operations.
///     Provides compile-time safety for permission checks.
/// </summary>
public sealed class OrdersPermission : Permission
{
    private OrdersPermission(string resource, string action, string? scope, string description)
        : base(resource, action, scope, description)
    {
    }

    /// <summary>Read orders (own orders)</summary>
    public static readonly OrdersPermission Read = new("orders", "read", null, "Read orders (own orders)");

    /// <summary>Read all orders (admin)</summary>
    public static readonly OrdersPermission ReadAll = new("orders", "read", "all", "Read all orders (admin)");

    /// <summary>Create orders</summary>
    public static readonly OrdersPermission Create = new("orders", "create", null, "Create orders");

    /// <summary>Process refunds</summary>
    public static readonly OrdersPermission Refund = new("orders", "refund", null, "Process refunds");

    /// <summary>Full management access to orders</summary>
    public static readonly OrdersPermission Manage = new("orders", "manage", null, "Full management access to orders");
}

/// <summary>
///     Strongly-typed permissions for entitlement operations.
///     Provides compile-time safety for permission checks.
/// </summary>
public sealed class EntitlementsPermission : Permission
{
    private EntitlementsPermission(string resource, string action, string? scope, string description)
        : base(resource, action, scope, description)
    {
    }

    /// <summary>View own entitlements</summary>
    public static readonly EntitlementsPermission ReadSelf = new("entitlements", "read", "self", "View own entitlements");

    /// <summary>View all entitlements (admin)</summary>
    public static readonly EntitlementsPermission ReadAll = new("entitlements", "read", "all", "View all entitlements (admin)");

    /// <summary>Grant entitlements</summary>
    public static readonly EntitlementsPermission Grant = new("entitlements", "grant", null, "Grant entitlements");

    /// <summary>Revoke entitlements</summary>
    public static readonly EntitlementsPermission Revoke = new("entitlements", "revoke", null, "Revoke entitlements");

    /// <summary>Full management access to entitlements</summary>
    public static readonly EntitlementsPermission Manage = new("entitlements", "manage", null, "Full management access to entitlements");
}
