/**
 * Tenant Provider Types
 *
 * Type definitions for multi-tenancy support.
 */

/**
 * Tenant provider interface
 */
export interface TenantProvider {
  /**
   * Get the current tenant ID
   * Return null if no tenant context
   */
  getTenantId(): Promise<string | null>;

  /**
   * Called when tenant context is missing but required
   */
  onTenantRequired?(): Promise<void>;
}

/**
 * Tenant configuration
 */
export interface TenantConfig {
  /** Tenant provider implementation */
  tenantProvider: TenantProvider;

  /** Header name for tenant ID (default: X-Tenant-Id) */
  headerName?: string;

  /** Whether tenant is required for all requests */
  required?: boolean;
}
