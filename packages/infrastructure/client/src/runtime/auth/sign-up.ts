import { createClient } from '../../client.js';
import { AuthModule } from '../../generated/modules/auth.gen.js';
import type { IdentityAuthenticationLocalSignUpInput, IdentityAuthenticationSignInOutput } from '../../generated/types.gen.js';
import { isApiError, isValidationError } from '../errors/guards.js';
import type { ApiError } from '../errors/types.js';
import type { Result } from '../result/types.js';
import { AuthServiceUnavailableError, MfaRequiredError, parseMfaEnrollmentOffer, SignUpError } from './errors.js';

/** Shared registration transport for the Next route and server action. */
export async function registerAccount(apiUrl: string, input: IdentityAuthenticationLocalSignUpInput): Promise<Record<string, unknown>> {
  // Retain older profile/claim extensions; declared fields still come from the
  // generated response validator and cannot be overridden by these extensions.
  const legacy: Record<string, unknown> = {};
  const auth = new AuthModule(
    createClient({
      baseUrl: apiUrl,
      autoRefresh: false,
      devtools: { enabled: false },
      interceptors: [
        {
          onResponse(response) {
            const value = response.data;
            if (value !== null && typeof value === 'object') {
              if ('roles' in value) legacy.roles = value.roles;
              if ('permissions' in value) legacy.permissions = value.permissions;
              if ('user' in value) legacy.user = value.user;
            }
            return Promise.resolve(response);
          },
        },
      ],
    }),
  );

  let response: Result<IdentityAuthenticationSignInOutput, ApiError>;
  try {
    response = await auth.postAuthSignUp(input);
  } catch (error) {
    if (isApiError(error) && error.metadata?.context === 'request') {
      throw new SignUpError('Invalid sign-up request');
    }
    throw new AuthServiceUnavailableError('Authentication service returned an invalid response.', error instanceof Error ? error : undefined);
  }
  if (!response.ok) {
    const error = response.error;
    if (error.status === 0 || error.status >= 500 || error.code === 'PARSE_ERROR') {
      throw new AuthServiceUnavailableError();
    }
    const message = typeof error.detail === 'string' ? error.detail : typeof error.message === 'string' ? error.message : 'Sign-up failed';
    throw new SignUpError(message, { fieldErrors: isValidationError(error) ? error.fieldErrors : undefined });
  }

  const data: Record<string, unknown> = { ...legacy, ...response.data };
  if (response.data.user && legacy.user !== null && typeof legacy.user === 'object') {
    data.user = { ...legacy.user, ...response.data.user };
  }
  if (data.requiresMfa) {
    throw new MfaRequiredError('Multi-factor authentication required', {
      mfaToken: typeof data.mfaToken === 'string' ? data.mfaToken : undefined,
      availableMethods: Array.isArray(data.availableMethods)
        ? data.availableMethods.filter((method): method is string => typeof method === 'string')
        : undefined,
      mfaEnrollment: parseMfaEnrollmentOffer(data.mfaEnrollment),
    });
  }
  if (data.success === false || data.requiresStepUp || typeof data.accessToken !== 'string' || !data.accessToken.trim()) {
    throw new AuthServiceUnavailableError('Authentication service returned an incomplete registration response.');
  }
  return data;
}
