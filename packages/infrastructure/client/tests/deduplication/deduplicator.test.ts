/**
 * Request Deduplication Tests
 * 
 * Tests for request deduplication functionality
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { RequestDeduplicator } from '../../src/runtime/deduplication/deduplicator.js';
import { ok, err } from '../../src/runtime/result/helpers.js';
import type { Result } from '../../src/runtime/result/types.js';
import type { ApiError } from '../../src/runtime/errors/types.js';

describe('Request Deduplication', () => {
  let deduplicator: RequestDeduplicator;

  beforeEach(() => {
    deduplicator = new RequestDeduplicator({ enabled: true });
  });

  describe('Deduplication', () => {
    it('should deduplicate identical GET requests', async () => {
      let executionCount = 0;
      const executor = async (): Promise<Result<string, ApiError>> => {
        executionCount++;
        await new Promise(resolve => setTimeout(resolve, 10));
        return ok('test-data');
      };

      // Fire two identical requests simultaneously
      const [result1, result2] = await Promise.all([
        deduplicator.deduplicate('GET', '/api/users', undefined, executor),
        deduplicator.deduplicate('GET', '/api/users', undefined, executor),
      ]);

      expect(executionCount).toBe(1); // Should only execute once
      expect(result1).toEqual(result2); // Should return same result
      expect(result1.ok).toBe(true);
      if (result1.ok) {
        expect(result1.data).toBe('test-data');
      }
    });

    it('should not deduplicate requests with different URLs', async () => {
      let executionCount = 0;
      const executor = async (): Promise<Result<string, ApiError>> => {
        executionCount++;
        return ok(`data-${executionCount}`);
      };

      const [result1, result2] = await Promise.all([
        deduplicator.deduplicate('GET', '/api/users/1', undefined, executor),
        deduplicator.deduplicate('GET', '/api/users/2', undefined, executor),
      ]);

      expect(executionCount).toBe(2); // Should execute both
      expect(result1.ok && result2.ok).toBe(true);
      if (result1.ok && result2.ok) {
        expect(result1.data).toBe('data-1');
        expect(result2.data).toBe('data-2');
      }
    });

    it('should not deduplicate requests with different methods', async () => {
      let executionCount = 0;
      const executor = async (): Promise<Result<string, ApiError>> => {
        executionCount++;
        return ok(`data-${executionCount}`);
      };

      const [result1, result2] = await Promise.all([
        deduplicator.deduplicate('GET', '/api/users', undefined, executor),
        deduplicator.deduplicate('POST', '/api/users', undefined, executor),
      ]);

      expect(executionCount).toBe(2); // Should execute both
    });

    it('should not deduplicate requests with different bodies', async () => {
      let executionCount = 0;
      const executor = async (): Promise<Result<string, ApiError>> => {
        executionCount++;
        return ok(`data-${executionCount}`);
      };

      const [result1, result2] = await Promise.all([
        deduplicator.deduplicate('POST', '/api/users', { name: 'Alice' }, executor),
        deduplicator.deduplicate('POST', '/api/users', { name: 'Bob' }, executor),
      ]);

      expect(executionCount).toBe(2); // Should execute both
    });

    it('should allow sequential requests after first completes', async () => {
      let executionCount = 0;
      const executor = async (): Promise<Result<string, ApiError>> => {
        executionCount++;
        await new Promise(resolve => setTimeout(resolve, 5));
        return ok(`data-${executionCount}`);
      };

      const result1 = await deduplicator.deduplicate('GET', '/api/users', undefined, executor);
      const result2 = await deduplicator.deduplicate('GET', '/api/users', undefined, executor);

      expect(executionCount).toBe(2); // Should execute both since they're sequential
    });

    it('should handle errors in deduplication', async () => {
      const testError: ApiError = {
        name: 'ApiError',
        code: 'NETWORK_ERROR',
        message: 'Network failed',
        status: 0,
      };

      const executor = async (): Promise<Result<string, ApiError>> => {
        await new Promise(resolve => setTimeout(resolve, 5));
        return err(testError);
      };

      const [result1, result2] = await Promise.all([
        deduplicator.deduplicate('GET', '/api/users', undefined, executor),
        deduplicator.deduplicate('GET', '/api/users', undefined, executor),
      ]);

      expect(result1.ok).toBe(false);
      expect(result2.ok).toBe(false);
      if (!result1.ok && !result2.ok) {
        expect(result1.error).toEqual(testError);
        expect(result2.error).toEqual(testError);
      }
    });

    it('should clean up after request completes', async () => {
      const executor = async (): Promise<Result<string, ApiError>> => {
        await new Promise(resolve => setTimeout(resolve, 5));
        return ok('test-data');
      };

      expect(deduplicator.pendingCount).toBe(0);

      const promise = deduplicator.deduplicate('GET', '/api/users', undefined, executor);
      
      // During execution
      expect(deduplicator.pendingCount).toBe(1);

      await promise;

      // After completion
      expect(deduplicator.pendingCount).toBe(0);
    });
  });

  describe('Custom Key Generator', () => {
    it('should use custom key generator', async () => {
      let executionCount = 0;

      // Custom generator that ignores body
      const customDeduplicator = new RequestDeduplicator({
        keyGenerator: (method, url) => `${method}:${url}`,
      });

      const executor = async (): Promise<Result<string, ApiError>> => {
        executionCount++;
        return ok('data');
      };

      // Same method and URL, different bodies - should deduplicate with custom generator
      const [result1, result2] = await Promise.all([
        customDeduplicator.deduplicate('POST', '/api/users', { name: 'Alice' }, executor),
        customDeduplicator.deduplicate('POST', '/api/users', { name: 'Bob' }, executor),
      ]);

      expect(executionCount).toBe(1); // Should only execute once
    });
  });

  describe('Clear', () => {
    it('should clear all pending requests', async () => {
      const executor = async (): Promise<Result<string, ApiError>> => {
        await new Promise(resolve => setTimeout(resolve, 100));
        return ok('test-data');
      };

      deduplicator.deduplicate('GET', '/api/users', undefined, executor);
      
      expect(deduplicator.pendingCount).toBe(1);
      
      deduplicator.clear();
      
      expect(deduplicator.pendingCount).toBe(0);
    });
  });
});
