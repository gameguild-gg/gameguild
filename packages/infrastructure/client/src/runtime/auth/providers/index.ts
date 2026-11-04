/**
 * Auth Providers
 *
 * Re-exports all provider factories and types.
 */

export { CredentialsProvider, type CredentialsProviderOptions } from './credentials';
export { DiscordProvider, type DiscordProviderOptions } from './discord';
export { GoogleProvider, type GoogleProviderOptions } from './google';
export { GitHubProvider, type GitHubProviderOptions } from './github';

export type { Provider, ProviderConfig, ProviderType, ProviderResult, CredentialsProviderConfig, OAuthProviderConfig } from './types';
