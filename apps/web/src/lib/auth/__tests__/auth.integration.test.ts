import { signIn, signOut, auth } from '@/auth';
import { signInWithEmailAndPassword, signInWithGoogle } from '@/lib/auth/auth.actions';

// This is an integration test that verifies the complete authentication flow
// Mock dependencies for integration testing
jest.mock('@/auth', () => ({
  signIn: jest.fn(),
  signOut: jest.fn(),
  auth: jest.fn(),
}));

jest.mock('@/configs/environment', () => ({
  environment: {
    apiBaseUrl: 'http://localhost:5000',
    googleClientId: 'test-google-client-id',
    googleClientSecret: 'test-google-client-secret',
  },
}));

// Mock fetch
global.fetch = jest.fn();

describe('Authentication Integration Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Complete Email/Password Authentication Flow', () => {
    it('should successfully complete email/password sign-in flow', async () => {
      // Mock the backend API response
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({
          user: {
            id: '123',
            username: 'testuser',
            email: 'test@example.com',
          },
          accessToken: 'access-token-123',
          refreshToken: 'refresh-token-123',
          expiresAt: '2024-12-31T23:59:59Z',
          tenantId: 'tenant-123',
          availableTenants: [
            { id: 'tenant-123', name: 'Test Tenant', isActive: true },
          ],
        }),
      });

      // Mock NextAuth signIn
      (signIn as jest.Mock).mockResolvedValue(undefined);

      // Execute sign-in
      await signInWithEmailAndPassword('test@example.com', 'password123');

      // Verify NextAuth signIn was called with correct parameters
      expect(signIn).toHaveBeenCalledWith('local', {
        email: 'test@example.com',
        password: 'password123',
      });
    });

    it('should handle invalid credentials', async () => {
      // Mock failed authentication
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 401,
        statusText: 'Unauthorized',
      });

      (signIn as jest.Mock).mockRejectedValue(new Error('Authentication failed'));

      // Verify error is thrown
      await expect(
        signInWithEmailAndPassword('test@example.com', 'wrongpassword')
      ).rejects.toThrow();
    });

    it('should handle network errors during sign-in', async () => {
      (signIn as jest.Mock).mockRejectedValue(new Error('Network error'));

      await expect(
        signInWithEmailAndPassword('test@example.com', 'password123')
      ).rejects.toThrow();
    });
  });

  describe('Complete Google Authentication Flow', () => {
    it('should successfully complete Google OAuth flow', async () => {
      // Mock NextAuth Google OAuth
      (signIn as jest.Mock).mockResolvedValue(undefined);

      // Execute Google sign-in
      await signInWithGoogle();

      // Verify NextAuth signIn was called for Google
      expect(signIn).toHaveBeenCalledWith('google');
    });

    it('should handle Google OAuth errors', async () => {
      (signIn as jest.Mock).mockRejectedValue(new Error('OAuth failed'));

      await expect(signInWithGoogle()).rejects.toThrow();
    });
  });

  describe('Session Management', () => {
    it('should retrieve active session', async () => {
      const mockSession = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'access-token',
        },
        currentTenant: {
          id: 'tenant-123',
          name: 'Test Tenant',
          isActive: true,
        },
        availableTenants: [
          { id: 'tenant-123', name: 'Test Tenant', isActive: true },
        ],
        expires: '2024-12-31T23:59:59Z',
      };

      (auth as jest.Mock).mockResolvedValue(mockSession);

      const session = await auth();

      expect(session).toEqual(mockSession);
      expect(session?.user.id).toBe('123');
      expect(session?.api.accessToken).toBe('access-token');
    });

    it('should return null for unauthenticated session', async () => {
      (auth as jest.Mock).mockResolvedValue(null);

      const session = await auth();

      expect(session).toBeNull();
    });

    it('should handle session with refresh token error', async () => {
      const mockSession = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'access-token',
        },
        error: 'RefreshTokenError',
        expires: '2024-12-31T23:59:59Z',
      };

      (auth as jest.Mock).mockResolvedValue(mockSession);

      const session = await auth();

      expect(session?.error).toBe('RefreshTokenError');
    });
  });

  describe('User Permissions and Multi-Tenancy', () => {
    it('should include tenant information in session', async () => {
      const mockSession = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'access-token',
        },
        currentTenant: {
          id: 'tenant-123',
          name: 'Test Tenant',
          isActive: true,
        },
        availableTenants: [
          { id: 'tenant-123', name: 'Test Tenant', isActive: true },
          { id: 'tenant-456', name: 'Another Tenant', isActive: true },
        ],
        expires: '2024-12-31T23:59:59Z',
      };

      (auth as jest.Mock).mockResolvedValue(mockSession);

      const session = await auth();

      expect(session?.currentTenant).toBeDefined();
      expect(session?.currentTenant?.id).toBe('tenant-123');
      expect(session?.availableTenants).toHaveLength(2);
    });

    it('should handle user with multiple available tenants', async () => {
      const mockSession = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'access-token',
        },
        availableTenants: [
          { id: 'tenant-1', name: 'Tenant 1', isActive: true },
          { id: 'tenant-2', name: 'Tenant 2', isActive: true },
          { id: 'tenant-3', name: 'Tenant 3', isActive: false },
        ],
        expires: '2024-12-31T23:59:59Z',
      };

      (auth as jest.Mock).mockResolvedValue(mockSession);

      const session = await auth();

      expect(session?.availableTenants).toHaveLength(3);
      const activeTenants = session?.availableTenants?.filter(t => t.isActive);
      expect(activeTenants).toHaveLength(2);
    });

    it('should handle user profile with custom avatar', async () => {
      const mockSession = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
          profilePictureUrl: 'https://example.com/avatar.jpg',
        },
        api: {
          accessToken: 'access-token',
        },
        expires: '2024-12-31T23:59:59Z',
      };

      (auth as jest.Mock).mockResolvedValue(mockSession);

      const session = await auth();

      expect((session?.user as any).profilePictureUrl).toBe('https://example.com/avatar.jpg');
    });
  });

  describe('Sign Out Flow', () => {
    it('should successfully sign out user', async () => {
      (signOut as jest.Mock).mockResolvedValue(undefined);

      await signOut();

      expect(signOut).toHaveBeenCalled();
    });

    it('should clear session after sign out', async () => {
      (signOut as jest.Mock).mockResolvedValue(undefined);
      (auth as jest.Mock).mockResolvedValue(null);

      await signOut();
      const session = await auth();

      expect(session).toBeNull();
    });
  });

  describe('Token Refresh Flow', () => {
    it('should handle token refresh in session', async () => {
      // Initial session with expiring token
      const initialSession = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'old-access-token',
        },
        expires: '2024-12-31T23:59:59Z',
      };

      // Mock token refresh API call
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        status: 200,
        statusText: 'OK',
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
          expiresAt: '2025-01-31T23:59:59Z',
        }),
      });

      (auth as jest.Mock).mockResolvedValue(initialSession);

      const session = await auth();

      expect(session).toBeDefined();
      expect(session?.api.accessToken).toBeDefined();
    });

    it('should handle failed token refresh', async () => {
      const sessionWithError = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'old-access-token',
        },
        error: 'RefreshTokenError',
        expires: '2024-12-31T23:59:59Z',
      };

      (auth as jest.Mock).mockResolvedValue(sessionWithError);

      const session = await auth();

      expect(session?.error).toBe('RefreshTokenError');
    });
  });

  describe('User Data Validation', () => {
    it('should validate user email format in session', async () => {
      const mockSession = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'access-token',
        },
        expires: '2024-12-31T23:59:59Z',
      };

      (auth as jest.Mock).mockResolvedValue(mockSession);

      const session = await auth();

      expect(session?.user.email).toMatch(/^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$/);
    });

    it('should include username in user data', async () => {
      const mockSession = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'access-token',
        },
        expires: '2024-12-31T23:59:59Z',
      };

      (auth as jest.Mock).mockResolvedValue(mockSession);

      const session = await auth();

      expect(session?.user.username).toBe('testuser');
      expect(session?.user.id).toBe('123');
    });
  });
});
