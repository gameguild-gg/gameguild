import { NextAuthConfig } from 'next-auth';
import { authConfig } from '../auth.config';
import { localSign, googleIdTokenSignIn } from '@/lib/auth/auth.actions';
import { User, Account } from 'next-auth';

// Mock dependencies
jest.mock('@/lib/auth/auth.actions', () => ({
  localSign: jest.fn(),
  googleIdTokenSignIn: jest.fn(),
  refreshAccessToken: jest.fn(),
}));

jest.mock('@/configs/environment', () => ({
  environment: {
    googleClientId: 'test-google-client-id',
    googleClientSecret: 'test-google-client-secret',
    apiBaseUrl: 'http://localhost:5000',
  },
}));

jest.mock('@/lib/utils/jwt-debug', () => ({
  logTokenDebugInfo: jest.fn(),
  shouldRefreshToken: jest.fn(() => false),
}));

describe('Auth Config', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Configuration', () => {
    it('should have correct pages configuration', () => {
      expect(authConfig.pages).toEqual({
        signIn: '/sign-in',
        error: '/auth/error',
      });
    });

    it('should use JWT session strategy', () => {
      expect(authConfig.session).toEqual({
        strategy: 'jwt',
      });
    });

    it('should have trustHost enabled', () => {
      expect(authConfig.trustHost).toBe(true);
    });

    it('should have debug enabled', () => {
      expect(authConfig.debug).toBe(true);
    });
  });

  describe('Providers', () => {
    it('should have local, GitHub, and Google providers', () => {
      expect(authConfig.providers).toBeDefined();
      expect(Array.isArray(authConfig.providers)).toBe(true);
      expect(authConfig.providers.length).toBeGreaterThanOrEqual(3);
    });
  });

  describe('Credentials Provider - authorize', () => {
    let credentialsProvider: any;

    beforeEach(() => {
      credentialsProvider = authConfig.providers.find((p: any) => p.id === 'local');
    });

    it('should throw error if email is missing', async () => {
      await expect(
        credentialsProvider.authorize({ password: 'password123' })
      ).rejects.toThrow('Email and password are required');
    });

    it('should throw error if password is missing', async () => {
      await expect(
        credentialsProvider.authorize({ email: 'test@example.com' })
      ).rejects.toThrow('Email and password are required');
    });

    it('should successfully authorize with valid credentials', async () => {
      const mockResponse = {
        user: {
          id: '123',
          email: 'test@example.com',
          username: 'testuser',
        },
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'tenant-123',
        availableTenants: [
          { id: 'tenant-123', name: 'Test Tenant', isActive: true },
        ],
      };

      (localSign as jest.Mock).mockResolvedValue(mockResponse);

      const user = await credentialsProvider.authorize({
        email: 'test@example.com',
        password: 'password123',
      });

      expect(user).toMatchObject({
        id: '123',
        email: 'test@example.com',
        name: 'testuser',
        username: 'testuser',
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'tenant-123',
        availableTenants: mockResponse.availableTenants,
      });
    });

    it('should throw error when localSign returns no user', async () => {
      (localSign as jest.Mock).mockResolvedValue({ user: null });

      await expect(
        credentialsProvider.authorize({
          email: 'test@example.com',
          password: 'wrongpassword',
        })
      ).rejects.toThrow('Invalid credentials');
    });

    it('should throw error when localSign fails', async () => {
      (localSign as jest.Mock).mockRejectedValue(new Error('API error'));

      await expect(
        credentialsProvider.authorize({
          email: 'test@example.com',
          password: 'password123',
        })
      ).rejects.toThrow('Invalid credentials');
    });
  });

  describe('Callbacks - redirect', () => {
    it('should handle relative URLs', async () => {
      const result = await authConfig.callbacks!.redirect!({
        url: '/dashboard',
        baseUrl: 'http://localhost:3000',
      });

      expect(result).toMatch(/\/dashboard$/);
    });

    it('should allow URLs starting with configured URL', async () => {
      process.env.NEXTAUTH_URL = 'http://localhost:3000';

      const result = await authConfig.callbacks!.redirect!({
        url: 'http://localhost:3000/dashboard',
        baseUrl: 'http://localhost:3000',
      });

      expect(result).toBe('http://localhost:3000/dashboard');
    });

    it('should redirect to configured base URL for other cases', async () => {
      process.env.NEXTAUTH_URL = 'http://localhost:3000';

      const result = await authConfig.callbacks!.redirect!({
        url: 'http://evil.com',
        baseUrl: 'http://localhost:3000',
      });

      expect(result).toBe('http://localhost:3000');
    });
  });

  describe('Callbacks - signIn', () => {
    it('should handle Google sign-in successfully', async () => {
      const mockResponse = {
        tenantId: 'tenant-456',
        accessToken: 'google-access-token',
        refreshToken: 'google-refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        availableTenants: [
          { id: 'tenant-456', name: 'Google Tenant', isActive: true },
        ],
      };

      (googleIdTokenSignIn as jest.Mock).mockResolvedValue(mockResponse);

      const user: User = {
        id: '456',
        email: 'google@example.com',
        name: 'Google User',
      };

      const account: Account = {
        provider: 'google',
        type: 'oauth',
        id_token: 'google-id-token',
        providerAccountId: '456',
      };

      const result = await authConfig.callbacks!.signIn!({ user, account });

      expect(result).toBe(true);
      expect(googleIdTokenSignIn).toHaveBeenCalledWith({ idToken: 'google-id-token' });
      expect(user).toMatchObject({
        tenantId: 'tenant-456',
        accessToken: 'google-access-token',
        refreshToken: 'google-refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        availableTenants: mockResponse.availableTenants,
      });
    });

    it('should return false for Google sign-in without ID token', async () => {
      const user: User = {
        id: '456',
        email: 'google@example.com',
      };

      const account: Account = {
        provider: 'google',
        type: 'oauth',
        providerAccountId: '456',
      };

      const result = await authConfig.callbacks!.signIn!({ user, account });

      expect(result).toBe(false);
    });

    it('should return false when Google sign-in fails', async () => {
      (googleIdTokenSignIn as jest.Mock).mockRejectedValue(new Error('API error'));

      const user: User = {
        id: '456',
        email: 'google@example.com',
      };

      const account: Account = {
        provider: 'google',
        type: 'oauth',
        id_token: 'google-id-token',
        providerAccountId: '456',
      };

      const result = await authConfig.callbacks!.signIn!({ user, account });

      expect(result).toBe(false);
    });

    it('should allow local authentication', async () => {
      const user: User = {
        id: '123',
        email: 'test@example.com',
      };

      const account: Account = {
        provider: 'local',
        type: 'credentials',
        providerAccountId: '123',
      };

      const result = await authConfig.callbacks!.signIn!({ user, account });

      expect(result).toBe(true);
    });
  });

  describe('Callbacks - jwt', () => {
    it('should create JWT on sign-in with valid auth data', async () => {
      const user: any = {
        id: '123',
        email: 'test@example.com',
        username: 'testuser',
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'tenant-123',
        availableTenants: [
          { id: 'tenant-123', name: 'Test Tenant', isActive: true },
        ],
      };

      const token: any = {};

      const result = await authConfig.callbacks!.jwt!({
        token,
        user,
        trigger: 'signIn',
      });

      expect(result).toMatchObject({
        id: '123',
        email: 'test@example.com',
        username: 'testuser',
        api: {
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
        },
        availableTenants: user.availableTenants,
        currentTenant: { id: 'tenant-123', name: 'Test Tenant', isActive: true },
        expiresAt: '2024-12-31T23:59:59Z',
      });
    });

    it('should return null on sign-in without auth data', async () => {
      const user: User = {
        id: '123',
        email: 'test@example.com',
      };

      const token: any = {};

      const result = await authConfig.callbacks!.jwt!({
        token,
        user,
        trigger: 'signIn',
      });

      expect(result).toBeNull();
    });

    it('should return null when tenant not in available tenants', async () => {
      const user: any = {
        id: '123',
        email: 'test@example.com',
        username: 'testuser',
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'wrong-tenant',
        availableTenants: [
          { id: 'tenant-123', name: 'Test Tenant', isActive: true },
        ],
      };

      const token: any = {};

      const result = await authConfig.callbacks!.jwt!({
        token,
        user,
        trigger: 'signIn',
      });

      expect(result).toBeNull();
    });

    it('should use Google profile picture when available', async () => {
      const user: any = {
        id: '123',
        email: 'test@example.com',
        username: 'testuser',
        image: 'https://google.com/profile.jpg',
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'tenant-123',
        availableTenants: [
          { id: 'tenant-123', name: 'Test Tenant', isActive: true },
        ],
      };

      const token: any = {};

      const result = await authConfig.callbacks!.jwt!({
        token,
        user,
        trigger: 'signIn',
      });

      expect(result?.profilePictureUrl).toBe('https://google.com/profile.jpg');
    });

    it('should generate DiceBear avatar when no profile picture', async () => {
      const user: any = {
        id: '123',
        email: 'test@example.com',
        username: 'testuser',
        name: 'Test User',
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'tenant-123',
        availableTenants: [
          { id: 'tenant-123', name: 'Test Tenant', isActive: true },
        ],
      };

      const token: any = {};

      const result = await authConfig.callbacks!.jwt!({
        token,
        user,
        trigger: 'signIn',
      });

      expect(result?.profilePictureUrl).toMatch(/dicebear\.com/);
      expect(result?.profilePictureUrl).toContain('Test User');
    });

    it('should handle sign-up trigger', async () => {
      const user: any = {
        id: '123',
        email: 'test@example.com',
        username: 'testuser',
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresAt: '2024-12-31T23:59:59Z',
        tenantId: 'tenant-123',
        availableTenants: [
          { id: 'tenant-123', name: 'Test Tenant', isActive: true },
        ],
      };

      const token: any = {};

      const result = await authConfig.callbacks!.jwt!({
        token,
        user,
        trigger: 'signUp',
      });

      expect(result).toBeDefined();
      expect(result?.id).toBe('123');
    });
  });
});
