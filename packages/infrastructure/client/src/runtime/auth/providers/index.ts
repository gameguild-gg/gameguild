/**
 * Auth Providers
 *
 * Re-exports all provider factories and types.
 */

export { CredentialsProvider, type CredentialsProviderOptions } from './credentials.js';
export { GoogleProvider, type GoogleProviderOptions } from './google.js';
export { GitHubProvider, type GitHubProviderOptions } from './github.js';
export { DiscordProvider, type DiscordProviderOptions } from './discord.js';

export type {
  Provider,
  ProviderConfig,
  ProviderType,
  ProviderResult,
  CredentialsProviderConfig,
  OAuthProviderConfig,
} from './types.js';
