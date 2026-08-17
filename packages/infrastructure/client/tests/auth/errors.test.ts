/**
 * Auth Error Types Tests
 */

import { describe, it, expect } from 'vitest';
import {
  AuthError,
  CredentialsSignInError,
  AccountLockedError,
  MfaRequiredError,
  SignUpError,
  SessionExpiredError,
  InvalidSessionError,
  TokenRefreshError,
  ConfigError,
  MissingSecretError,
  ProviderNotFoundError,
  CSRFError,
  OAuthError,
  OAuthCallbackError,
  MfaVerificationError,
  PasswordResetError,
  EmailVerificationError,
  SessionTerminationError,
  parseErrorBody,
  extractErrorMessage,
  isAuthError,
  isReauthRequired,
  isCredentialsError,
} from '../../src/runtime/auth/errors.js';

describe('Auth Error Types', () => {
  describe('AuthError', () => {
    it('should create with message', () => {
      const error = new AuthError('test error');
      expect(error.message).toBe('test error');
      expect(error.name).toBe('AuthError');
      expect(error.type).toBe('AuthError');
      expect(error.status).toBe(500);
    });

    it('should accept options', () => {
      const cause = new Error('cause');
      const error = new AuthError('test', {
        type: 'CustomType',
        status: 418,
        cause,
      });
      expect(error.type).toBe('CustomType');
      expect(error.status).toBe(418);
      expect(error.cause).toBe(cause);
    });

    it('should serialize to JSON', () => {
      const error = new AuthError('test error', { type: 'TestType', status: 400 });
      const json = error.toJSON();
      expect(json).toEqual({
        error: 'TestType',
        message: 'test error',
        status: 400,
      });
    });

    it('should be instanceof Error', () => {
      const error = new AuthError('test');
      expect(error).toBeInstanceOf(Error);
      expect(error).toBeInstanceOf(AuthError);
    });
  });

  describe('CredentialsSignInError', () => {
    it('should have correct defaults', () => {
      const error = new CredentialsSignInError();
      expect(error.message).toBe('Invalid credentials');
      expect(error.name).toBe('CredentialsSignInError');
      expect(error.type).toBe('CredentialsSignin');
      expect(error.status).toBe(401);
    });

    it('should accept custom message', () => {
      const error = new CredentialsSignInError('Wrong password');
      expect(error.message).toBe('Wrong password');
    });
  });

  describe('AccountLockedError', () => {
    it('should have correct defaults', () => {
      const error = new AccountLockedError();
      expect(error.message).toBe('Account is locked');
      expect(error.name).toBe('AccountLockedError');
      expect(error.type).toBe('AccountLocked');
      expect(error.status).toBe(403);
    });
  });

  describe('MfaRequiredError', () => {
    it('should have correct defaults', () => {
      const error = new MfaRequiredError();
      expect(error.message).toBe('Multi-factor authentication required');
      expect(error.type).toBe('MfaRequired');
      expect(error.status).toBe(403);
    });

    it('should store MFA session info', () => {
      const error = new MfaRequiredError('MFA needed', {
        mfaSessionId: 'session-123',
        availableMethods: ['totp', 'sms'],
      });
      expect(error.mfaSessionId).toBe('session-123');
      expect(error.availableMethods).toEqual(['totp', 'sms']);
    });
  });

  describe('SignUpError', () => {
    it('should have correct defaults', () => {
      const error = new SignUpError();
      expect(error.message).toBe('Sign-up failed');
      expect(error.type).toBe('SignUpError');
      expect(error.status).toBe(400);
    });

    it('should store field errors', () => {
      const error = new SignUpError('Validation failed', {
        fieldErrors: {
          email: ['Email is required'],
          password: ['Too short', 'Must contain uppercase'],
        },
      });
      expect(error.fieldErrors?.email).toEqual(['Email is required']);
      expect(error.fieldErrors?.password).toHaveLength(2);
    });
  });

  describe('SessionExpiredError', () => {
    it('should have correct defaults', () => {
      const error = new SessionExpiredError();
      expect(error.message).toBe('Session has expired');
      expect(error.type).toBe('SessionExpired');
      expect(error.status).toBe(401);
    });
  });

  describe('InvalidSessionError', () => {
    it('should have correct defaults', () => {
      const error = new InvalidSessionError();
      expect(error.message).toBe('Invalid session');
      expect(error.type).toBe('InvalidSession');
      expect(error.status).toBe(401);
    });
  });

  describe('TokenRefreshError', () => {
    it('should have correct defaults', () => {
      const error = new TokenRefreshError();
      expect(error.message).toBe('Token refresh failed');
      expect(error.type).toBe('TokenRefreshError');
      expect(error.status).toBe(401);
    });

    it('should store cause', () => {
      const cause = new Error('network failure');
      const error = new TokenRefreshError('Refresh failed', cause);
      expect(error.cause).toBe(cause);
    });
  });

  describe('ConfigError', () => {
    it('should have correct properties', () => {
      const error = new ConfigError('Invalid config');
      expect(error.message).toBe('Invalid config');
      expect(error.type).toBe('Configuration');
      expect(error.status).toBe(500);
    });
  });

  describe('MissingSecretError', () => {
    it('should have default message about AUTH_SECRET', () => {
      const error = new MissingSecretError();
      expect(error.message).toContain('AUTH_SECRET');
      expect(error.name).toBe('MissingSecretError');
    });
  });

  describe('ProviderNotFoundError', () => {
    it('should include provider name in message', () => {
      const error = new ProviderNotFoundError('custom-oauth');
      expect(error.message).toContain('custom-oauth');
      expect(error.type).toBe('ProviderNotFound');
      expect(error.status).toBe(400);
    });
  });

  describe('CSRFError', () => {
    it('should have correct defaults', () => {
      const error = new CSRFError();
      expect(error.message).toBe('CSRF token validation failed');
      expect(error.type).toBe('CSRFError');
      expect(error.status).toBe(403);
    });
  });

  describe('OAuthError', () => {
    it('should store message and cause', () => {
      const cause = new Error('upstream');
      const error = new OAuthError('OAuth failed', cause);
      expect(error.message).toBe('OAuth failed');
      expect(error.type).toBe('OAuthError');
      expect(error.status).toBe(500);
      expect(error.cause).toBe(cause);
    });
  });

  describe('OAuthCallbackError', () => {
    it('should have correct defaults', () => {
      const error = new OAuthCallbackError();
      expect(error.message).toBe('Invalid OAuth callback');
      expect(error.type).toBe('OAuthCallbackError');
      expect(error.status).toBe(400);
    });
  });

  describe('MfaVerificationError', () => {
    it('should have correct defaults', () => {
      const error = new MfaVerificationError();
      expect(error.message).toBe('MFA verification failed');
      expect(error.type).toBe('MfaVerificationError');
      expect(error.status).toBe(401);
    });

    it('should store attempts remaining', () => {
      const error = new MfaVerificationError('Wrong code', {
        attemptsRemaining: 2,
      });
      expect(error.attemptsRemaining).toBe(2);
    });
  });

  describe('PasswordResetError', () => {
    it('should have correct defaults', () => {
      const error = new PasswordResetError();
      expect(error.message).toBe('Password reset failed');
      expect(error.status).toBe(400);
    });
  });

  describe('EmailVerificationError', () => {
    it('should have correct defaults', () => {
      const error = new EmailVerificationError();
      expect(error.message).toBe('Email verification failed');
      expect(error.status).toBe(400);
    });
  });

  describe('SessionTerminationError', () => {
    it('should have correct defaults', () => {
      const error = new SessionTerminationError();
      expect(error.message).toBe('Session termination failed');
      expect(error.status).toBe(500);
    });
  });
});

describe('Error Helpers', () => {
  describe('parseErrorBody', () => {
    it('should parse JSON response body', async () => {
      const response = new Response(JSON.stringify({ message: 'error' }), {
        status: 400,
      });
      const body = await parseErrorBody(response);
      expect(body).toEqual({ message: 'error' });
    });

    it('should return empty object for non-JSON response', async () => {
      const response = new Response('not json', { status: 500 });
      const body = await parseErrorBody(response);
      expect(body).toEqual({});
    });
  });

  describe('extractErrorMessage', () => {
    it('should extract message field', () => {
      expect(extractErrorMessage({ message: 'test' }, 'fallback')).toBe('test');
    });

    it('should extract detail field', () => {
      expect(extractErrorMessage({ detail: 'test detail' }, 'fallback')).toBe('test detail');
    });

    it('should prefer message over detail', () => {
      expect(extractErrorMessage({ message: 'msg', detail: 'dtl' }, 'fallback')).toBe('msg');
    });

    it('should return fallback when no recognized field', () => {
      expect(extractErrorMessage({}, 'fallback')).toBe('fallback');
    });
  });

  describe('isAuthError', () => {
    it('should return true for AuthError instances', () => {
      expect(isAuthError(new AuthError('test'))).toBe(true);
      expect(isAuthError(new CredentialsSignInError())).toBe(true);
      expect(isAuthError(new TokenRefreshError())).toBe(true);
    });

    it('should return false for non-AuthError', () => {
      expect(isAuthError(new Error('test'))).toBe(false);
      expect(isAuthError('string')).toBe(false);
      expect(isAuthError(null)).toBe(false);
      expect(isAuthError(undefined)).toBe(false);
    });
  });

  describe('isReauthRequired', () => {
    it('should return true for session/token errors', () => {
      expect(isReauthRequired(new SessionExpiredError())).toBe(true);
      expect(isReauthRequired(new InvalidSessionError())).toBe(true);
      expect(isReauthRequired(new TokenRefreshError())).toBe(true);
    });

    it('should return false for other auth errors', () => {
      expect(isReauthRequired(new CredentialsSignInError())).toBe(false);
      expect(isReauthRequired(new AccountLockedError())).toBe(false);
      expect(isReauthRequired(new MfaRequiredError())).toBe(false);
    });

    it('should return false for non-auth errors', () => {
      expect(isReauthRequired(new Error('test'))).toBe(false);
    });
  });

  describe('isCredentialsError', () => {
    it('should return true for CredentialsSignInError', () => {
      expect(isCredentialsError(new CredentialsSignInError())).toBe(true);
    });

    it('should return false for other errors', () => {
      expect(isCredentialsError(new AuthError('test'))).toBe(false);
      expect(isCredentialsError(new Error('test'))).toBe(false);
    });
  });
});
