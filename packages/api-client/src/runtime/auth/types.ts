/**
 * Authentication Types
 *
 * Type definitions for the pluggable authentication system.
 */

/**
 * Token pair returned from authentication
 */
export interface TokenPair {
  /** JWT access token */
  accessToken: string;
  /** Refresh token (if using refresh flow) */
  refreshToken?: string;
  /** Token expiry in seconds */
  expiresIn?: number;
  /** Token type (always 'Bearer') */
  tokenType: 'Bearer';
  /** OAuth scopes granted */
  scope?: string;
}

/**
 * Token provider interface
 *
 * Implement this to provide tokens from any authentication source.
 */
export interface TokenProvider {
  /**
   * Get the current access token
   * Return null if not authenticated
   */
  getAccessToken(): Promise<string | null>;

  /**
   * Get the refresh token (optional)
   * Used for automatic token refresh
   */
  getRefreshToken?(): Promise<string | null>;

  /**
   * Called when tokens are refreshed
   * Store the new tokens in your auth system
   */
  onTokenRefresh?(tokens: TokenPair): Promise<void>;

  /**
   * Called when authentication is required
   * Typically redirect to login page
   */
  onAuthenticationRequired?(): Promise<void>;
}

/**
 * Authentication configuration
 */
export interface AuthConfig {
  /** Token provider implementation */
  tokenProvider: TokenProvider;

  /** Enable automatic token refresh */
  autoRefresh?: boolean;

  /** Refresh token before this many seconds until expiry */
  refreshThreshold?: number;

  /** Maximum number of refresh retries */
  maxRefreshRetries?: number;
}

/**
 * Auth mode for requests
 */
export type AuthMode = 'required' | 'optional' | 'none';
