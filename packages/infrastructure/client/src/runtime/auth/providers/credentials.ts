/**
 * Credentials Provider
 *
 * Authenticates via email/password against the .NET backend's /v1/auth/sign-in endpoint.
 * This is the primary provider for local authentication.
 *
 * @example
 * ```typescript
 * import { GameGuildAuth } from '@game-guild/client/next';
 * import { CredentialsProvider } from '@game-guild/client/auth';
 *
 * export const { handlers, auth, signIn, signOut, signUp } = GameGuildAuth({
 *   providers: [
 *     CredentialsProvider({
 *       apiUrl: process.env.API_URL!,
 *     }),
 *   ],
 * });
 * ```
 */

import type { CredentialsProviderConfig, ProviderResult, SessionUser } from '../types.js';
import { AccountLockedError, AuthServiceUnavailableError, CredentialsSignInError, MfaRequiredError, parseMfaEnrollmentOffer } from '../errors.js';
import { resolveAuthPermissions, resolveAuthRoles } from '../claims.js';
import { createClient } from '../../../client.js';
import { AuthModule } from '../../../generated/modules/auth.gen.js';
import { postAuthSignInEndpoint } from '../../../generated/endpoints.gen.js';
import type {
  IdentityAuthenticationCompleteMfaSignInInput,
  IdentityAuthenticationLocalSignInInput,
  IdentityAuthenticationSignInOutput,
} from '../../../generated/types.gen.js';
import { isApiError } from '../../errors/guards.js';
import type { ApiError } from '../../errors/types.js';
import type { Result } from '../../result/types.js';

/**
 * Options for the credentials provider
 */
export interface CredentialsProviderOptions {
  /**
   * Backend API base URL.
   * If not provided, uses the apiUrl from the main config.
   */
  apiUrl?: string;

  /**
   * Sign-in endpoint path
   * @default '/v1/auth/sign-in'
   */
  signInPath?: string;

  /**
   * Custom authorize function override.
   * If provided, replaces the default .NET sign-in logic.
   */
  authorize?: CredentialsProviderConfig['authorize'];
}

/**
 * Create a credentials provider for email/password authentication.
 *
 * @param options - Provider configuration
 * @returns A credentials provider config
 */
export function CredentialsProvider(options: CredentialsProviderOptions = {}): CredentialsProviderConfig {
  const { signInPath = postAuthSignInEndpoint.path, authorize: customAuthorize } = options;

  return {
    id: 'credentials',
    name: 'Credentials',
    type: 'credentials',
    authorize:
      customAuthorize ??
      (async (credentials) => {
        // The apiUrl can come from options or from the main config
        // When called from the auth system, the apiUrl is injected
        const apiUrl = options.apiUrl || (credentials.__apiUrl as string);

        if (!apiUrl) {
          throw new CredentialsSignInError('API URL not configured. Set apiUrl in provider options or GameGuildAuth config.');
        }

        const email = credentials.email as string;
        const password = credentials.password as string;
        const tenantId = credentials.tenantId as string | undefined;
        // "Remember me" opt-in from the sign-in form: drives the backend's persistent
        // refresh-token lifetime and this client's cookie storage policy.
        const rememberMe = credentials.rememberMe === true;
        // Cloudflare Turnstile token (issue #1082): presented only after the API flagged
        // repeated failed sign-ins from this network. Forwarded verbatim; the API
        // verifies it server-side and ignores it when no challenge is required.
        const turnstileToken = credentials.turnstileToken as string | undefined;

        const completingMfa = Object.hasOwn(credentials, 'mfaToken');
        if (!completingMfa && (!email || !password)) {
          throw new CredentialsSignInError('Email and password are required');
        }

        let mfaInput: IdentityAuthenticationCompleteMfaSignInInput | undefined;
        const signInInput: IdentityAuthenticationLocalSignInInput = { email, password };
        if (completingMfa) {
          const { mfaToken, method, code } = credentials;
          if (
            typeof mfaToken !== 'string' ||
            !/^[A-Za-z0-9_-]{43}$/.test(mfaToken) ||
            (method !== 'Totp' && method !== 'BackupCode') ||
            typeof code !== 'string' ||
            !code.trim()
          ) {
            throw new CredentialsSignInError('A valid MFA challenge, method and code are required');
          }
          mfaInput = { mfaToken, method, code };
        } else {
          if (tenantId) signInInput.tenantId = tenantId;
          if (rememberMe) signInInput.rememberMe = true;
          if (turnstileToken) signInInput.turnstileToken = turnstileToken;
        }

        // Older integrations may return extra profile fields and claim arrays. Preserve that
        // compatibility while the generated module validates every declared field.
        const legacyClaims: Record<string, unknown> = {};
        const auth = new AuthModule(
          createClient({
            baseUrl: apiUrl,
            autoRefresh: false,
            devtools: { enabled: false },
            interceptors: [
              {
                onRequest(request) {
                  return Promise.resolve(request.path === postAuthSignInEndpoint.path ? { ...request, path: signInPath } : request);
                },
                onResponse(response) {
                  const value = response.data;
                  if (value !== null && typeof value === 'object') {
                    if ('roles' in value) legacyClaims.roles = value.roles;
                    if ('permissions' in value) legacyClaims.permissions = value.permissions;
                    if ('user' in value) legacyClaims.user = value.user;
                  }
                  return Promise.resolve(response);
                },
              },
            ],
          }),
        );

        let response: Result<IdentityAuthenticationSignInOutput, ApiError>;
        try {
          response = mfaInput ? await auth.postAuthMfaSignInComplete(mfaInput) : await auth.postAuthSignIn(signInInput);
        } catch (error) {
          if (isApiError(error) && error.metadata?.context === 'request') {
            throw new CredentialsSignInError('Invalid sign-in request');
          }
          throw new AuthServiceUnavailableError('Authentication service returned an invalid response.', error instanceof Error ? error : undefined);
        }

        if (!response.ok) {
          if (response.error.status >= 500 || response.error.status === 0 || response.error.code === 'PARSE_ERROR') {
            throw new AuthServiceUnavailableError();
          }

          if (response.error.status === 423) {
            throw new AccountLockedError(response.error.message);
          }

          throw new CredentialsSignInError(response.error.message);
        }

        const data: Record<string, unknown> = { ...legacyClaims, ...response.data };
        if (response.data.user && legacyClaims.user !== null && typeof legacyClaims.user === 'object') {
          data.user = { ...legacyClaims.user, ...response.data.user };
        }

        // Check for MFA requirement
        if (data.requiresMfa) {
          throw new MfaRequiredError('Multi-factor authentication required', {
            mfaSessionId: data.mfaSessionId as string | undefined,
            mfaToken: typeof data.mfaToken === 'string' ? data.mfaToken : undefined,
            availableMethods: Array.isArray(data.availableMethods) ? data.availableMethods.filter((m): m is string => typeof m === 'string') : undefined,
            mfaEnrollment: parseMfaEnrollmentOffer(data.mfaEnrollment),
          });
        }

        if (typeof data.accessToken !== 'string' || !data.accessToken.trim()) {
          throw new CredentialsSignInError('Authentication did not complete');
        }

        // Extract user info from the response
        const backendUser = data.user as Record<string, unknown> | undefined;
        const user: SessionUser = {
          /* v8 ignore next */
          id: (data.userId as string) || (backendUser?.id as string) || '',
          email: (data.email as string) || (backendUser?.email as string) || (completingMfa ? '' : email),
          name: (backendUser?.displayName as string) || (backendUser?.username as string) || null,
          image: (backendUser?.profilePictureUrl as string) || null,
          roles: resolveAuthRoles(data, backendUser),
          permissions: resolveAuthPermissions(data, backendUser),
        };

        const result: ProviderResult = {
          tokens: {
            accessToken: data.accessToken,
            refreshToken: data.refreshToken as string,
            expiresIn: data.expiresIn as number | undefined,
            accessTokenExpiresAt: data.accessTokenExpiresAt as string | undefined,
            refreshTokenExpiresAt: data.refreshTokenExpiresAt as string | undefined,
            tokenType: 'Bearer',
          },
          user,
          sessionId: data.sessionId as string | undefined,
          tenantId: data.tenantId as string | undefined,
          availableTenants: data.availableTenants as Array<{ id: string; name: string }> | undefined,
          rememberMe,
          ...(completingMfa && Array.isArray(data.mfaEnrollmentBackupCodes)
            ? { mfaEnrollmentBackupCodes: data.mfaEnrollmentBackupCodes.filter((c): c is string => typeof c === 'string') }
            : {}),
        };

        return result;
      }),
  };
}
