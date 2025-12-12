import { isUserWithAuthData, hasRefreshTokenError } from '../auth.utils';
import { User, Session } from 'next-auth';

describe('Auth Utils', () => {
  describe('isUserWithAuthData', () => {
    it('should return true when user has tenantId', () => {
      const user: User & { tenantId: string } = {
        id: '123',
        email: 'test@example.com',
        tenantId: 'tenant-123',
      };

      expect(isUserWithAuthData(user)).toBe(true);
    });

    it('should return true when user has accessToken', () => {
      const user: User & { accessToken: string } = {
        id: '123',
        email: 'test@example.com',
        accessToken: 'access-token',
      };

      expect(isUserWithAuthData(user)).toBe(true);
    });

    it('should return true when user has refreshToken', () => {
      const user: User & { refreshToken: string } = {
        id: '123',
        email: 'test@example.com',
        refreshToken: 'refresh-token',
      };

      expect(isUserWithAuthData(user)).toBe(true);
    });

    it('should return true when user has all auth data', () => {
      const user: User & { tenantId: string; accessToken: string; refreshToken: string } = {
        id: '123',
        email: 'test@example.com',
        tenantId: 'tenant-123',
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
      };

      expect(isUserWithAuthData(user)).toBe(true);
    });

    it('should return false when user has no auth data', () => {
      const user: User = {
        id: '123',
        email: 'test@example.com',
      };

      expect(isUserWithAuthData(user)).toBe(false);
    });

    it('should return false when user only has basic properties', () => {
      const user: User = {
        id: '123',
        email: 'test@example.com',
        name: 'Test User',
        username: 'testuser',
      };

      expect(isUserWithAuthData(user)).toBe(false);
    });
  });

  describe('hasRefreshTokenError', () => {
    it('should return true when session has RefreshTokenError', () => {
      const session: Session = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'token',
        },
        error: 'RefreshTokenError',
        expires: '2024-12-31T23:59:59Z',
      };

      expect(hasRefreshTokenError(session)).toBe(true);
    });

    it('should return false when session has CorruptedSessionError', () => {
      const session: Session = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'token',
        },
        error: 'CorruptedSessionError',
        expires: '2024-12-31T23:59:59Z',
      };

      expect(hasRefreshTokenError(session)).toBe(false);
    });

    it('should return false when session has no error', () => {
      const session: Session = {
        user: {
          id: '123',
          username: 'testuser',
          email: 'test@example.com',
        },
        api: {
          accessToken: 'token',
        },
        expires: '2024-12-31T23:59:59Z',
      };

      expect(hasRefreshTokenError(session)).toBe(false);
    });

    it('should return false when session is null', () => {
      expect(hasRefreshTokenError(null)).toBe(false);
    });
  });
});
