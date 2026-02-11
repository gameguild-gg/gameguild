/**
 * JWT Encryption/Decryption Tests
 */

import { describe, it, expect } from 'vitest';
import { encodeJWT, decodeJWT } from '../../src/runtime/auth/jwt.js';
import type { JWTPayload } from '../../src/runtime/auth/types.js';

const TEST_SECRET = 'test-secret-key-that-is-at-least-32-characters-long';

describe('JWT Encryption/Decryption', () => {
  describe('encodeJWT', () => {
    it('should encrypt a JWT payload', async () => {
      const token: JWTPayload = {
        user: { id: 'user-1', email: 'test@example.com' },
        accessToken: 'access-123',
        refreshToken: 'refresh-456',
        accessTokenExpires: Date.now() + 3600000,
      };

      const encrypted = await encodeJWT({ token, secret: TEST_SECRET });

      expect(encrypted).toBeTruthy();
      expect(typeof encrypted).toBe('string');
      // JWE tokens have 5 parts separated by dots
      expect(encrypted.split('.').length).toBe(5);
    });

    it('should produce different outputs for different payloads', async () => {
      const token1: JWTPayload = {
        user: { id: 'user-1' },
        accessToken: 'token-1',
        refreshToken: '',
        accessTokenExpires: Date.now(),
      };
      const token2: JWTPayload = {
        user: { id: 'user-2' },
        accessToken: 'token-2',
        refreshToken: '',
        accessTokenExpires: Date.now(),
      };

      const enc1 = await encodeJWT({ token: token1, secret: TEST_SECRET });
      const enc2 = await encodeJWT({ token: token2, secret: TEST_SECRET });

      expect(enc1).not.toBe(enc2);
    });

    it('should respect maxAge parameter', async () => {
      const token: JWTPayload = {
        user: { id: 'user-1' },
        accessToken: 'access',
        refreshToken: 'refresh',
        accessTokenExpires: Date.now() + 60000,
      };

      const encrypted = await encodeJWT({
        token,
        secret: TEST_SECRET,
        maxAge: 3600,
      });

      expect(encrypted).toBeTruthy();

      // Verify by decoding
      const decoded = await decodeJWT({ token: encrypted, secret: TEST_SECRET });
      expect(decoded).not.toBeNull();
      const now = Math.floor(Date.now() / 1000);
      expect(decoded!.exp).toBeGreaterThan(now);
      expect(decoded!.exp).toBeLessThanOrEqual(now + 3600 + 2);
    });
  });

  describe('decodeJWT', () => {
    it('should decrypt a JWT back to original payload', async () => {
      const original: JWTPayload = {
        user: { id: 'user-1', email: 'test@example.com', name: 'Test' },
        accessToken: 'access-123',
        refreshToken: 'refresh-456',
        accessTokenExpires: Date.now() + 3600000,
        sessionId: 'session-abc',
        tenantId: 'tenant-xyz',
      };

      const encrypted = await encodeJWT({ token: original, secret: TEST_SECRET });
      const decoded = await decodeJWT({ token: encrypted, secret: TEST_SECRET });

      expect(decoded).not.toBeNull();
      expect(decoded!.user.id).toBe('user-1');
      expect(decoded!.user.email).toBe('test@example.com');
      expect(decoded!.accessToken).toBe('access-123');
      expect(decoded!.refreshToken).toBe('refresh-456');
      expect(decoded!.sessionId).toBe('session-abc');
      expect(decoded!.tenantId).toBe('tenant-xyz');
    });

    it('should return null for empty token', async () => {
      const result = await decodeJWT({ token: '', secret: TEST_SECRET });
      expect(result).toBeNull();
    });

    it('should return null for invalid token', async () => {
      const result = await decodeJWT({ token: 'invalid.token.here', secret: TEST_SECRET });
      expect(result).toBeNull();
    });

    it('should return null for wrong secret', async () => {
      const token: JWTPayload = {
        user: { id: 'user-1' },
        accessToken: 'access',
        refreshToken: 'refresh',
        accessTokenExpires: Date.now() + 3600000,
      };

      const encrypted = await encodeJWT({ token, secret: TEST_SECRET });
      const decoded = await decodeJWT({
        token: encrypted,
        secret: 'completely-different-secret-that-is-long-enough',
      });

      expect(decoded).toBeNull();
    });

    it('should return null for tampered token', async () => {
      const token: JWTPayload = {
        user: { id: 'user-1' },
        accessToken: 'access',
        refreshToken: 'refresh',
        accessTokenExpires: Date.now() + 3600000,
      };

      const encrypted = await encodeJWT({ token, secret: TEST_SECRET });
      // Tamper with the token
      const tampered = encrypted.slice(0, -10) + 'XXXXXXXXXX';

      const decoded = await decodeJWT({ token: tampered, secret: TEST_SECRET });
      expect(decoded).toBeNull();
    });

    it('should return null for expired JWT', async () => {
      const token: JWTPayload = {
        user: { id: 'user-1' },
        accessToken: 'access',
        refreshToken: 'refresh',
        accessTokenExpires: Date.now(),
        iat: Math.floor(Date.now() / 1000) - 120,
        exp: Math.floor(Date.now() / 1000) - 60, // expired 60s ago
      };

      const encrypted = await encodeJWT({ token, secret: TEST_SECRET });
      // The 15s clock tolerance might cover very recently expired, so use a clearly expired one
      const decoded = await decodeJWT({ token: encrypted, secret: TEST_SECRET });

      expect(decoded).toBeNull();
    });
  });

  describe('roundtrip', () => {
    it('should preserve all user fields', async () => {
      const original: JWTPayload = {
        user: {
          id: 'u1',
          email: 'a@b.com',
          name: 'Alice',
          image: 'https://example.com/avatar.png',
          roles: ['admin', 'user'],
          permissions: ['read', 'write', 'delete'],
        },
        accessToken: 'at',
        refreshToken: 'rt',
        accessTokenExpires: Date.now() + 3600000,
        availableTenants: [
          { id: 't1', name: 'Tenant 1' },
          { id: 't2', name: 'Tenant 2' },
        ],
      };

      const encrypted = await encodeJWT({ token: original, secret: TEST_SECRET });
      const decoded = await decodeJWT({ token: encrypted, secret: TEST_SECRET });

      expect(decoded!.user.roles).toEqual(['admin', 'user']);
      expect(decoded!.user.permissions).toEqual(['read', 'write', 'delete']);
      expect(decoded!.availableTenants).toEqual([
        { id: 't1', name: 'Tenant 1' },
        { id: 't2', name: 'Tenant 2' },
      ]);
    });

    it('should set iat and exp automatically', async () => {
      const now = Math.floor(Date.now() / 1000);

      const original: JWTPayload = {
        user: { id: 'u1' },
        accessToken: 'at',
        refreshToken: 'rt',
        accessTokenExpires: Date.now() + 3600000,
      };

      const encrypted = await encodeJWT({ token: original, secret: TEST_SECRET });
      const decoded = await decodeJWT({ token: encrypted, secret: TEST_SECRET });

      expect(decoded!.iat).toBeGreaterThanOrEqual(now - 1);
      expect(decoded!.iat).toBeLessThanOrEqual(now + 2);
      // Default maxAge is 30 days
      expect(decoded!.exp).toBeGreaterThan(now + 86400);
    });
  });
});
