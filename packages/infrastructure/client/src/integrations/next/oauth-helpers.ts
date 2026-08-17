/**
 * OAuth Provider Type-Safe Helpers
 *
 * Provides type-safe access to optional OAuth provider methods
 * (exchangeToken, getAuthorizeUrl, handleCallback) without
 * resorting to `as Function` or `as Record<string, unknown>` casts.
 *
 * This addresses the Interface Segregation Principle — OAuth providers
 * may implement different subsets of methods depending on their flow.
 */

import type { ProviderResult, OAuthProviderConfig } from '../../runtime/auth/types.js';

/**
 * Extended OAuth provider that may have custom bridge methods for
 * communicating with the .NET backend.
 */
export interface OAuthProviderWithMethods extends OAuthProviderConfig {
  /**
   * Exchange an ID token (e.g., Google) for backend tokens.
   * Used for providers where the client obtains the token directly.
   */
  exchangeToken?: (
    idToken: string,
    apiUrl: string,
    tenantId?: string
  ) => Promise<ProviderResult>;

  /**
   * Get the authorization URL from the backend.
   * Used for providers where the backend manages the OAuth state.
   */
  getAuthorizeUrl?: (
    apiUrl: string,
    redirectUri?: string
  ) => Promise<string>;

  /**
   * Handle the OAuth callback by sending the code to the backend.
   * Used for authorization-code flow providers.
   * The redirect URI and tenant are optional so providers with shorter
   * signatures remain structurally compatible.
   */
  handleCallback?: (
    apiUrl: string,
    code: string,
    state?: string,
    redirectUri?: string,
    tenantId?: string
  ) => Promise<ProviderResult>;
}

/**
 * Type-safe accessor for exchangeToken method.
 * Returns the bound method or undefined if not available.
 */
export function getOAuthExchangeToken(
  provider: OAuthProviderWithMethods
): OAuthProviderWithMethods['exchangeToken'] {
  return typeof provider.exchangeToken === 'function'
    ? provider.exchangeToken
    : undefined;
}

/**
 * Type-safe accessor for getAuthorizeUrl method.
 */
export function getOAuthAuthorizeUrl(
  provider: OAuthProviderWithMethods
): OAuthProviderWithMethods['getAuthorizeUrl'] {
  return typeof provider.getAuthorizeUrl === 'function'
    ? provider.getAuthorizeUrl
    : undefined;
}

/**
 * Type-safe accessor for handleCallback method.
 */
export function getOAuthHandleCallback(
  provider: OAuthProviderWithMethods
): OAuthProviderWithMethods['handleCallback'] {
  return typeof provider.handleCallback === 'function'
    ? provider.handleCallback
    : undefined;
}
