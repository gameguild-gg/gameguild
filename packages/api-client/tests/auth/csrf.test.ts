/**
 * CSRF Protection Tests
 */

import { describe, it, expect } from 'vitest';
import { createCSRFToken, validateCSRFToken } from '../../src/runtime/auth/csrf.js';

describe('CSRF Protection', () => {
  describe('createCSRFToken', () => {
    it('should create a token pair with cookie and token', async () => {
      const result = await createCSRFToken('test-secret');

      expect(result).toHaveProperty('cookie');
      expect(result).toHaveProperty('token');
      expect(result.cookie).toBeTruthy();
      expect(result.token).toBeTruthy();
    });

    it('should create cookie in format "randomValue|hash"', async () => {
      const result = await createCSRFToken('test-secret');

      const parts = result.cookie.split('|');
      expect(parts.length).toBe(2);
      expect(parts[0]!.length).toBeGreaterThan(0);
      expect(parts[1]!.length).toBeGreaterThan(0);
    });

    it('should create different tokens on each call', async () => {
      const result1 = await createCSRFToken('test-secret');
      const result2 = await createCSRFToken('test-secret');

      expect(result1.cookie).not.toBe(result2.cookie);
      expect(result1.token).not.toBe(result2.token);
    });

    it('should produce hash as the token', async () => {
      const result = await createCSRFToken('test-secret');

      // Token should be the SHA-256 hash (64 hex chars)
      expect(result.token.length).toBe(64);
      expect(/^[a-f0-9]+$/.test(result.token)).toBe(true);
    });

    it('should include hash in cookie value', async () => {
      const result = await createCSRFToken('test-secret');

      const [, hashPart] = result.cookie.split('|');
      expect(hashPart).toBe(result.token);
    });
  });

  describe('validateCSRFToken', () => {
    it('should validate a correctly created token', async () => {
      const { cookie, token } = await createCSRFToken('test-secret');

      const isValid = await validateCSRFToken(cookie, token, 'test-secret');

      expect(isValid).toBe(true);
    });

    it('should reject null cookie value', async () => {
      const isValid = await validateCSRFToken(null, 'some-token', 'secret');
      expect(isValid).toBe(false);
    });

    it('should reject undefined cookie value', async () => {
      const isValid = await validateCSRFToken(undefined, 'some-token', 'secret');
      expect(isValid).toBe(false);
    });

    it('should reject null body token', async () => {
      const isValid = await validateCSRFToken('cookie-value', null, 'secret');
      expect(isValid).toBe(false);
    });

    it('should reject undefined body token', async () => {
      const isValid = await validateCSRFToken('cookie-value', undefined, 'secret');
      expect(isValid).toBe(false);
    });

    it('should reject cookie without pipe separator', async () => {
      const isValid = await validateCSRFToken('no-separator', 'token', 'secret');
      expect(isValid).toBe(false);
    });

    it('should reject tampered cookie hash', async () => {
      const { cookie, token } = await createCSRFToken('test-secret');
      const [randomValue] = cookie.split('|');

      const tamperedCookie = `${randomValue}|tampered-hash`;
      const isValid = await validateCSRFToken(tamperedCookie, token, 'test-secret');

      expect(isValid).toBe(false);
    });

    it('should reject wrong body token', async () => {
      const { cookie } = await createCSRFToken('test-secret');

      const isValid = await validateCSRFToken(cookie, 'wrong-token', 'test-secret');

      expect(isValid).toBe(false);
    });

    it('should reject with different secret', async () => {
      const { cookie, token } = await createCSRFToken('secret-1');

      const isValid = await validateCSRFToken(cookie, token, 'secret-2');

      expect(isValid).toBe(false);
    });

    it('should reject empty cookie parts', async () => {
      const isValid = await validateCSRFToken('|hash', 'token', 'secret');
      expect(isValid).toBe(false);
    });
  });
});
