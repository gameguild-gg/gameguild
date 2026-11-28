/**
 * Error Transformation Tests - Basic coverage
 */

import { describe, it, expect } from 'vitest';
import { createApiError, createNetworkError } from '../../src/runtime/errors/transform.js';

describe('Error Transformation', () => {
  describe('createApiError', () => {
    it('should create ApiError from Response with JSON', async () => {
      const response = new Response(
        JSON.stringify({
          title: 'Validation Failed',
          status: 400,
        }),
        {
          status: 400,
          statusText: 'Bad Request',
          headers: { 'Content-Type': 'application/json' },
        }
      );

      const error = await createApiError(response);

      expect(error.code).toBe('VALIDATION_ERROR');
      expect(error.status).toBe(400);
    });

    it('should create ApiError from Response without body', async () => {
      const response = new Response(null, {
        status: 500,
        statusText: 'Internal Server Error',
      });

      const error = await createApiError(response);

      expect(error.code).toBe('SERVER_ERROR');
      expect(error.status).toBe(500);
    });

    it('should handle 401 Unauthorized', async () => {
      const response = new Response(null, {
        status: 401,
        statusText: 'Unauthorized',
      });

      const error = await createApiError(response);

      expect(error.code).toBe('AUTHENTICATION_ERROR');
      expect(error.status).toBe(401);
    });

    it('should handle 403 Forbidden', async () => {
      const response = new Response(null, {
        status: 403,
        statusText: 'Forbidden',
      });

      const error = await createApiError(response);

      expect(error.code).toBe('FORBIDDEN');
      expect(error.status).toBe(403);
    });

    it('should handle 404 Not Found', async () => {
      const response = new Response(null, {
        status: 404,
        statusText: 'Not Found',
      });

      const error = await createApiError(response);

      expect(error.code).toBe('NOT_FOUND');
      expect(error.status).toBe(404);
    });

    it('should handle 429 Rate Limited', async () => {
      const response = new Response(null, {
        status: 429,
        statusText: 'Too Many Requests',
      });

      const error = await createApiError(response);

      expect(error.code).toBe('RATE_LIMITED');
      expect(error.status).toBe(429);
    });

    it('should surface a bare error body as the message, as the MFA endpoints answer', async () => {
      const response = new Response(JSON.stringify({ error: 'Invalid or expired email code' }), {
        status: 400,
        statusText: 'Bad Request',
        headers: { 'Content-Type': 'application/json' },
      });

      const error = await createApiError(response);

      expect(error.message).toBe('Invalid or expired email code');
      expect(error.status).toBe(400);
    });

    it('should still prefer ProblemDetails fields over a bare error body', async () => {
      const response = new Response(JSON.stringify({ title: 'Conflict', error: 'ignored' }), {
        status: 400,
        statusText: 'Bad Request',
        headers: { 'Content-Type': 'application/json' },
      });

      const error = await createApiError(response);

      expect(error.message).toBe('Conflict');
    });

    it('should preserve the cooling-off payload of a 425 so the caller can read the remaining wait', async () => {
      const response = new Response(JSON.stringify({ success: false, message: 'Too early', retryAfterSeconds: 4200 }), {
        status: 425,
        statusText: 'Too Early',
        headers: { 'Content-Type': 'application/json' },
      });

      const error = await createApiError(response);

      expect(error.status).toBe(425);
      expect(error.metadata?.body).toEqual({ success: false, message: 'Too early', retryAfterSeconds: 4200 });
    });
  });

  describe('createNetworkError', () => {
    it('should create NETWORK_ERROR from Error', () => {
      const originalError = new Error('Connection failed');
      const error = createNetworkError(originalError);

      expect(error.code).toBe('NETWORK_ERROR');
      expect(error.status).toBe(0);
      expect(error.message).toContain('Network error');
    });

    it('should create TIMEOUT from AbortError', () => {
      const abortError = new Error('Aborted');
      abortError.name = 'AbortError';

      const error = createNetworkError(abortError);

      expect(error.code).toBe('TIMEOUT');
      expect(error.status).toBe(0);
      expect(error.message).toContain('timed out');
    });

    it('should create NETWORK_ERROR from unknown cause', () => {
      const error = createNetworkError('Connection failed');

      expect(error.code).toBe('NETWORK_ERROR');
      expect(error.status).toBe(0);
    });
  });
});
