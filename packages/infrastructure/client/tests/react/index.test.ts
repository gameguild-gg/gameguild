/**
 * Tests for React Integration Index
 * 
 * Tests the stub hook implementations and type exports.
 */

import { describe, it, expect } from 'vitest';
import {
  useApiClient,
  useQuery,
  useMutation,
  useOptimisticUpdate,
  ApiClientContext,
} from '../../src/integrations/react/index.js';

describe('React Integration Stubs', () => {
  describe('ApiClientContext', () => {
    it('should have a displayName', () => {
      expect(ApiClientContext.displayName).toBe('ApiClientContext');
    });
  });

  describe('useApiClient', () => {
    it('should throw when called without React', () => {
      expect(() => useApiClient()).toThrow(
        'useApiClient requires React'
      );
    });
  });

  describe('useQuery', () => {
    it('should throw when called without React', () => {
      expect(() =>
        useQuery(['key'], async () => ({ ok: true, data: null } as any))
      ).toThrow('useQuery requires React');
    });
  });

  describe('useMutation', () => {
    it('should throw when called without React', () => {
      expect(() =>
        useMutation(async () => ({ ok: true, data: null } as any))
      ).toThrow('useMutation requires React');
    });
  });

  describe('useOptimisticUpdate', () => {
    it('should throw when called without React', () => {
      expect(() => useOptimisticUpdate(['key'])).toThrow(
        'useOptimisticUpdate requires React'
      );
    });
  });
});
