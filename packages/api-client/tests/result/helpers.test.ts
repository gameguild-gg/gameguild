/**
 * Result Helpers Tests
 * 
 * Tests for Result monad helper functions
 */

import { describe, it, expect } from 'vitest';
import { ok, err, isOk, isErr, unwrap, unwrapOr, match } from '../../src/runtime/result/helpers.js';
import type { Result } from '../../src/runtime/result/types.js';

describe('Result Helpers', () => {
  describe('ok', () => {
    it('should create successful result', () => {
      const result = ok(42);

      expect(result.ok).toBe(true);
      if (result.ok) {
        expect(result.data).toBe(42);
      }
    });

    it('should create result with object data', () => {
      const data = { id: '123', name: 'Test' };
      const result = ok(data);

      expect(result.ok).toBe(true);
      if (result.ok) {
        expect(result.data).toEqual(data);
      }
    });

    it('should create result with null data', () => {
      const result = ok(null);

      expect(result.ok).toBe(true);
      if (result.ok) {
        expect(result.data).toBeNull();
      }
    });
  });

  describe('err', () => {
    it('should create error result', () => {
      const error = { message: 'Test error', code: 'ERROR' };
      const result = err(error);

      expect(result.ok).toBe(false);
      if (!result.ok) {
        expect(result.error).toEqual(error);
      }
    });

    it('should create result with string error', () => {
      const result = err('Something went wrong');

      expect(result.ok).toBe(false);
      if (!result.ok) {
        expect(result.error).toBe('Something went wrong');
      }
    });
  });

  describe('isOk', () => {
    it('should return true for successful results', () => {
      const result = ok(42);
      expect(isOk(result)).toBe(true);
    });

    it('should return false for error results', () => {
      const result = err('error');
      expect(isOk(result)).toBe(false);
    });

    it('should narrow type correctly', () => {
      const result: Result<number, string> = ok(42);

      if (isOk(result)) {
        // TypeScript should know result.data is number here
        expect(result.data).toBe(42);
      }
    });
  });

  describe('isErr', () => {
    it('should return true for error results', () => {
      const result = err('error');
      expect(isErr(result)).toBe(true);
    });

    it('should return false for successful results', () => {
      const result = ok(42);
      expect(isErr(result)).toBe(false);
    });

    it('should narrow type correctly', () => {
      const result: Result<number, string> = err('failed');

      if (isErr(result)) {
        // TypeScript should know result.error is string here
        expect(result.error).toBe('failed');
      }
    });
  });

  describe('unwrap', () => {
    it('should return data for successful results', () => {
      const result = ok(42);
      expect(unwrap(result)).toBe(42);
    });

    it('should throw for error results', () => {
      const result = err('Something went wrong');
      expect(() => unwrap(result)).toThrow('Something went wrong');
    });

    it('should throw with error object message', () => {
      const result = err({ message: 'Custom error', code: 'ERR' });
      expect(() => unwrap(result)).toThrow();
    });
  });

  describe('unwrapOr', () => {
    it('should return data for successful results', () => {
      const result = ok(42);
      expect(unwrapOr(result, 0)).toBe(42);
    });

    it('should return default for error results', () => {
      const result = err('error');
      expect(unwrapOr(result, 0)).toBe(0);
    });

    it('should work with object defaults', () => {
      const defaultValue = { id: 'default', name: 'Default' };
      const result: Result<{ id: string; name: string }, string> = err('error');

      expect(unwrapOr(result, defaultValue)).toEqual(defaultValue);
    });

    it('should work with null defaults', () => {
      const result: Result<string, string> = err('error');
      expect(unwrapOr(result, null)).toBeNull();
    });
  });

  describe('match', () => {
    it('should call ok handler for successful results', () => {
      const result = ok(42);
      const output = match(result, {
        ok: (data) => `Success: ${data}`,
        err: (error) => `Error: ${error}`,
      });

      expect(output).toBe('Success: 42');
    });

    it('should call err handler for error results', () => {
      const result = err('Something failed');
      const output = match(result, {
        ok: (data) => `Success: ${data}`,
        err: (error) => `Error: ${error}`,
      });

      expect(output).toBe('Error: Something failed');
    });

    it('should work with different return types', () => {
      const result = ok(42);
      const number = match(result, {
        ok: (data) => data * 2,
        err: () => 0,
      });

      expect(number).toBe(84);
    });

    it('should work with complex transformations', () => {
      type User = { id: string; name: string };
      type UserError = { code: string; message: string };

      const result: Result<User, UserError> = ok({ id: '123', name: 'Alice' });

      const greeting = match(result, {
        ok: (user) => `Hello, ${user.name}!`,
        err: (error) => `Error ${error.code}: ${error.message}`,
      });

      expect(greeting).toBe('Hello, Alice!');
    });

    it('should handle void returns', () => {
      let sideEffect = '';

      const result = ok(42);
      match(result, {
        ok: (data) => { sideEffect = `Got ${data}`; },
        err: () => { sideEffect = 'Error'; },
      });

      expect(sideEffect).toBe('Got 42');
    });
  });

  describe('Result Type Guards in Practice', () => {
    it('should work in if-else chains', () => {
      const result: Result<number, string> = ok(42);

      if (isOk(result)) {
        expect(result.data).toBe(42);
      } else {
        throw new Error('Should not reach here');
      }
    });

    it('should work with early returns', () => {
      function processResult(result: Result<number, string>): number {
        if (isErr(result)) {
          return -1;
        }
        return result.data * 2;
      }

      expect(processResult(ok(21))).toBe(42);
      expect(processResult(err('error'))).toBe(-1);
    });

    it('should compose with other helpers', () => {
      const result = ok(42);

      const value = isOk(result) 
        ? unwrap(result) 
        : unwrapOr(result, 0);

      expect(value).toBe(42);
    });
  });
});
