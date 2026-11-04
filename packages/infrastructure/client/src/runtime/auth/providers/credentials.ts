/**
 * Credentials Provider
 *
 * Authenticates via email/password against the .NET backend's sign-in contract
 * through the generated API client (`AuthModule.postAuthSignIn` /
 * `AuthModule.postAuthMfaSignInComplete`), so request and response shapes stay
 * inside the typed OpenAPI contract (issue #1217, findings F3/F4 — the Turnstile
 * hardening of #1082 shipped through a hand-rolled fetch and widened that gap).
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

import type { CredentialsProviderConfig, ProviderResult, SessionUser } from '../types';
import { AccountLockedError, AuthServiceUnavailableError, CredentialsSignInError, MfaRequiredError, parseMfaEnrollmentOffer } from '../errors';
import { resolveAuthPermissions, resolveAuthRoles } from '../claims';
import { AuthModule } from '../../../generated/modules/auth.gen';
import { createServerClient } from '../../../server';

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
   * @deprecated The provider always calls the generated client method for
   * `POST /v1/auth/sign-in`; the path is kept only for backward-compatible
   * configuration typing.
   */
  signInPath?: string;

  /**
   * Custom authorize function override.
   * If provided, replaces the default .NET sign-in logic.
   */
  authorize?: CredentialsProviderConfig['authorize'];
}

/**
 * Create a credentials provider for email/password.
 *
 * @param options - Provider configuration
 * @returns A credentials provider config
 */
export function CredentialsProvider(options: CredentialsProviderOptions = {}): CredentialsProviderConfig {
  const { authorize: customAuthorize } = options;

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
        // repeated failed sign-ins from this network. Forwarded verbatim through the
        // generated contract field; the API verifies it server-side and ignores it
        // when no challenge is required.
        const turnstileToken = credentials.turnstileToken as string | undefined;

        const completingMfa = Object.hasOwn(credentials, 'mfaToken');
        if (!completingMfa && (!email || !password)) {
          throw new CredentialsSignInError('Email and password are required');
        }

        const { mfaToken, method, code } = credentials;
        if (
          completingMfa &&
          (typeof mfaToken !== 'string' ||
            !/^[A-Za-z0-9_-]{43}$/.test(mfaToken) ||
            (method !== 'Totp' && method !== 'BackupCode') ||
            typeof code !== 'string' ||
            !code.trim())
        ) {
          throw new CredentialsSignInError('A valid MFA challenge, method and code are required');
        }

        const auth = new AuthModule(
          createServerClient({
            baseUrl: apiUrl,
            auth: { getAccessToken: () => Promise.resolve(null) },
          }),
        );

        let result: Awaited<ReturnType<AuthModule['postAuthSignIn']>>;
        try {
          result = completingMfa
            ? await auth.postAuthMfaSignInComplete({
                mfaToken: mfaToken as string,
                method: method as 'Totp' | 'BackupCode',
                code: code as string,
              })
            : await auth.postAuthSignIn({
                email,
                password,
                ...(tenantId ? { tenantId } : {}),
                ...(rememberMe ? { rememberMe: true } : {}),
                ...(turnstileToken ? { turnstileToken } : {}),
              });
        } catch (error) {
          // Unreachable API or generated-client contract drift (safeParse throws,
          // issue #864): either way the authentication service cannot be trusted
          // to have completed sign-in.
          throw new AuthServiceUnavailableError(
            'Authentication service is unreachable. Please check the API deployment.',
            error instanceof Error ? error : undefined,
          );
        }

        if (!result.ok) {
          const status = result.error.status ?? 0;
          const message = result.error.detail || result.error.message || 'Invalid credentials';

          if (status === 0 || status >= 500) {
            throw new AuthServiceUnavailableError();
          }

          if (status === 423) {
            throw new AccountLockedError(message);
          }

          throw new CredentialsSignInError(message);
        }

        const data = result.data;

        // Check for MFA requirement
        if (data?.requiresMfa) {
          throw new MfaRequiredError('Multi-factor authentication required', {
            mfaSessionId: data.mfaSessionId ?? undefined,
            mfaToken: typeof data.mfaToken === 'string' ? data.mfaToken : undefined,
            availableMethods: Array.isArray(data.availableMethods) ? data.availableMethods.filter((m): m is string => typeof m === 'string') : undefined,
            mfaEnrollment: parseMfaEnrollmentOffer(data.mfaEnrollment),
          });
        }

        if (typeof data?.accessToken !== 'string' || !data.accessToken.trim()) {
          throw new CredentialsSignInError('Authentication did not complete');
        }

        // Extract user info from the response. The claims helpers read the raw
        // response record and the decoded access-token payload for roles and
        // permissions, so they keep receiving the untyped view of the payload.
        const rawData = data as unknown as Record<string, unknown>;
        const backendUser = rawData.user as Record<string, unknown> | undefined;
        const user: SessionUser = {
          /* v8 ignore next */
          id: data.userId || (backendUser?.id as string) || '',
          email: data.email || (backendUser?.email as string) || (completingMfa ? '' : email),
          name: (backendUser?.displayName as string) || (backendUser?.username as string) || null,
          image: (backendUser?.profilePictureUrl as string) || null,
          roles: resolveAuthRoles(rawData, backendUser),
          permissions: resolveAuthPermissions(rawData, backendUser),
        };

        const resultPayload: ProviderResult = {
          tokens: {
            accessToken: data.accessToken,
            refreshToken: data.refreshToken ?? undefined,
            expiresIn: data.expiresIn,
            accessTokenExpiresAt: data.accessTokenExpiresAt,
            refreshTokenExpiresAt: data.refreshTokenExpiresAt,
            tokenType: 'Bearer',
          },
          user,
          sessionId: data.sessionId,
          tenantId: data.tenantId ?? undefined,
          availableTenants: data.availableTenants as Array<{ id: string; name: string }> | undefined,
          rememberMe,
          ...(completingMfa && Array.isArray(data.mfaEnrollmentBackupCodes)
            ? { mfaEnrollmentBackupCodes: data.mfaEnrollmentBackupCodes.filter((c): c is string => typeof c === 'string') }
            : {}),
        };

        return resultPayload;
      }),
  };
}
