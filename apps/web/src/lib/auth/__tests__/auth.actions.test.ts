import { signInWithEmailAndPassword, signInWithGoogle, localSign, googleIdTokenSignIn, refreshAccessToken } from '../auth.actions';
import { signIn } from '@/auth';
import { AuthError } from 'next-auth';
import { environment } from '@/configs/environment';

// Mock dependencies
jest.mock('@/auth', () => ({
  signIn: jest.fn(),
  signOut: jest.fn(),
}));

jest.mock('@/configs/environment', () => ({
  environment: {
    apiBaseUrl: 'http://localhost:5000',
  },
}));

// Mock global fetch
global.fetch = jest.fn();

describe('Authentication Actions', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('signInWithEmailAndPassword', () => {
    it('should successfully sign in with valid credentials', async () => {
      (signIn as jest.Mock).mockResolvedValue(undefined);

      await expect(signInWithEmailAndPassword('test@example.com', 'password123')).resolves.not.toThrow();

      expect(signIn).toHaveBeenCalledWith('local', {
        email: 'test@example.com',
        password: 'password123',
      });
    });

    it('should throw error for OAuthSignInError', async () => {
      const authError = new AuthError('OAuthSignInError');
      (signIn as jest.Mock).mockRejectedValue(authError);

      await expect(signInWithEmailAndPassword('test@example.com', 'password123')).rejects.toThrow(
        'OAuth sign-in failed'
      );
    });

    it('should throw error for OAuthCallbackError', async () => {
      const authError = new AuthError('OAuthCallbackError');
      (signIn as jest.Mock).mockRejectedValue(authError);

      await expect(signInWithEmailAndPassword('test@example.com', 'password123')).rejects.toThrow(
        'OAuth callback error'
      );
    });

    it('should throw error for AccessDenied', async () => {
      const authError = new AuthError('AccessDenied');
      (signIn as jest.Mock).mockRejectedValue(authError);

      await expect(signInWithEmailAndPassword('test@example.com', 'password123')).rejects.toThrow('Access denied');
    });

    it('should throw error for OAuthAccountNotLinked', async () => {
      const authError = new AuthError('OAuthAccountNotLinked');
      (signIn as jest.Mock).mockRejectedValue(authError);

      await expect(signInWithEmailAndPassword('test@example.com', 'password123')).rejects.toThrow(
        'Email already in use with different provider'
      );
    });

    it('should throw generic error for unknown AuthError types', async () => {
      const authError = new AuthError('UnknownError');
      (signIn as jest.Mock).mockRejectedValue(authError);

      await expect(signInWithEmailAndPassword('test@example.com', 'password123')).rejects.toThrow(
        'Authentication error occurred'
      );
    });

    it('should re-throw non-AuthError errors', async () => {
      const customError = new Error('Custom error');
      (signIn as jest.Mock).mockRejectedValue(customError);

      await expect(signInWithEmailAndPassword('test@example.com', 'password123')).rejects.toThrow('Custom error');
    });
  });

  describe('signInWithGoogle', () => {
    it('should successfully initiate Google sign-in', async () => {
      (signIn as jest.Mock).mockResolvedValue(undefined);

      await expect(signInWithGoogle()).resolves.not.toThrow();

      expect(signIn).toHaveBeenCalledWith('google');
    });

    it('should handle OAuth sign-in errors', async () => {
      const authError = new AuthError('OAuthSignInError');
      (signIn as jest.Mock).mockRejectedValue(authError);

      await expect(signInWithGoogle()).rejects.toThrow('OAuth sign-in failed');
    });

    it('should handle OAuth callback errors', async () => {
      const authError = new AuthError('OAuthCallbackError');
      (signIn as jest.Mock).mockRejectedValue(authError);

      await expect(signInWithGoogle()).rejects.toThrow('OAuth callback error');
    });

    it('should handle access denied errors', async () => {
      const authError = new AuthError('AccessDenied');
      (signIn as jest.Mock).mockRejectedValue(authError);

      await expect(signInWithGoogle()).rejects.toThrow('Access denied');
    });

    it('should handle account not linked errors', async () => {
      const authError = new AuthError('OAuthAccountNotLinked');
      (signIn as jest.Mock).mockRejectedValue(authError);

      await expect(signInWithGoogle()).rejects.toThrow('Email already in use with different provider');
    });
  });

  describe('localSign', () => {
    it('should successfully authenticate with local credentials', async () => {
      const mockResponse = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'tenant-123',
        availableTenants: [
          { id: 'tenant-123', name: 'Test Tenant', isActive: true },
        ],
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      const result = await localSign({
        email: 'test@example.com',
        password: 'password123',
      });

      expect(result).toEqual(mockResponse);
      expect(global.fetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/auth/sign-in',
        expect.objectContaining({
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            email: 'test@example.com',
            password: 'password123',
          }),
        })
      );
    });

    it('should throw error when API returns non-ok response', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 401,
        statusText: 'Unauthorized',
      });

      await expect(
        localSign({
          email: 'test@example.com',
          password: 'wrongpassword',
        })
      ).rejects.toThrow('Failed to authenticate with local credentials');
    });

    it('should handle network errors', async () => {
      (global.fetch as jest.Mock).mockRejectedValue(new Error('Network error'));

      await expect(
        localSign({
          email: 'test@example.com',
          password: 'password123',
        })
      ).rejects.toThrow('Failed to authenticate with local credentials');
    });

    it('should strip trailing slash from API base URL', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ user: { id: '123' }, accessToken: 'token', refreshToken: 'refresh', expiresAt: '2024-12-31T23:59:59Z' }),
      });

      await localSign({
        email: 'test@example.com',
        password: 'password123',
      });

      expect(global.fetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/auth/sign-in',
        expect.any(Object)
      );
    });
  });

  describe('googleIdTokenSignIn', () => {
    it('should successfully authenticate with Google ID token', async () => {
      const mockResponse = {
        user: {
          id: '456',
          username: 'googleuser',
          email: 'google@example.com',
        },
        accessToken: 'google-access-token',
        refreshToken: 'google-refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'tenant-456',
        availableTenants: [
          { id: 'tenant-456', name: 'Google Tenant', isActive: true },
        ],
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockResponse,
      });

      const result = await googleIdTokenSignIn({
        idToken: 'google-id-token',
      });

      expect(result).toEqual(mockResponse);
      expect(global.fetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/auth/google',
        expect.objectContaining({
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            idToken: 'google-id-token',
            tenantId: undefined,
          }),
        })
      );
    });

    it('should include tenantId when provided', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ user: { id: '456' }, accessToken: 'token', refreshToken: 'refresh', expiresAt: '2024-12-31T23:59:59Z' }),
      });

      await googleIdTokenSignIn({
        idToken: 'google-id-token',
        tenantId: 'specific-tenant',
      });

      expect(global.fetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/auth/google',
        expect.objectContaining({
          body: JSON.stringify({
            idToken: 'google-id-token',
            tenantId: 'specific-tenant',
          }),
        })
      );
    });

    it('should throw error when API returns non-ok response', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 401,
        statusText: 'Unauthorized',
      });

      await expect(
        googleIdTokenSignIn({
          idToken: 'invalid-token',
        })
      ).rejects.toThrow('Failed to authenticate with Google ID token');
    });

    it('should handle network errors', async () => {
      (global.fetch as jest.Mock).mockRejectedValue(new Error('Network error'));

      await expect(
        googleIdTokenSignIn({
          idToken: 'google-id-token',
        })
      ).rejects.toThrow('Failed to authenticate with Google ID token');
    });
  });

  describe('refreshAccessToken', () => {
    it('should successfully refresh access token', async () => {
      const mockResponse = {
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'tenant-123',
      };

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => mockResponse,
      });

      const result = await refreshAccessToken('valid-refresh-token');

      expect(result).toEqual(mockResponse);
      expect(global.fetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/auth/refresh',
        expect.objectContaining({
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
          },
          body: JSON.stringify({
            refreshToken: 'valid-refresh-token',
          }),
        })
      );
    });

    it('should throw error for empty refresh token', async () => {
      await expect(refreshAccessToken('')).rejects.toThrow('Refresh token is empty or null');
    });

    it('should throw error for null refresh token', async () => {
      await expect(refreshAccessToken(null as any)).rejects.toThrow('Refresh token is empty or null');
    });

    it('should trim whitespace from refresh token', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ accessToken: 'token', refreshToken: 'refresh', expiresAt: '2024-12-31T23:59:59Z' }),
      });

      await refreshAccessToken('  token-with-spaces  ');

      expect(global.fetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/auth/refresh',
        expect.objectContaining({
          body: JSON.stringify({
            refreshToken: 'token-with-spaces',
          }),
        })
      );
    });

    it('should handle 401 Unauthorized response', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 401,
        statusText: 'Unauthorized',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ error: 'Invalid refresh token' }),
      });

      await expect(refreshAccessToken('invalid-token')).rejects.toThrow();
    });

    it('should handle network errors', async () => {
      (global.fetch as jest.Mock).mockRejectedValue(new Error('Network error'));

      await expect(refreshAccessToken('valid-token')).rejects.toThrow();
    });

    it('should handle non-JSON response', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
        headers: new Headers({ 'content-type': 'text/plain' }),
        text: async () => 'Server error',
      });

      await expect(refreshAccessToken('valid-token')).rejects.toThrow();
    });
  });
});
