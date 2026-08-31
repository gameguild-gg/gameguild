/**
 * Result Helpers
 *
 * Utility functions for working with Result types.
 */

import type { Result, Ok, Err } from './types.js';

/**
 * Create a success result
 */
export function ok<T>(data: T): Ok<T> {
  return { ok: true, data };
}

/**
 * Create an error result
 */
export function err<E>(error: E): Err<E> {
  return { ok: false, error };
}

/**
 * Check if a result is successful
 */
export function isOk<T, E>(result: Result<T, E>): result is Ok<T> {
  return result.ok === true;
}

/**
 * Check if a result is an error
 */
export function isErr<T, E>(result: Result<T, E>): result is Err<E> {
  return result.ok === false;
}

/**
 * Unwrap a result, throwing if it's an error
 */
export function unwrap<T, E>(result: Result<T, E>): T {
  if (result.ok) {
    return result.data;
  }
  throw result.error;
}

/**
 * Unwrap a result with a default value
 */
export function unwrapOr<T, E>(result: Result<T, E>, defaultValue: T): T {
  if (result.ok) {
    return result.data;
  }
  return defaultValue;
}

/**
 * Unwrap a result with a function to provide default
 */
export function unwrapOrElse<T, E>(result: Result<T, E>, fn: (error: E) => T): T {
  if (result.ok) {
    return result.data;
  }
  return fn(result.error);
}

/**
 * Map the success value of a result
 */
export function map<T, U, E>(result: Result<T, E>, fn: (value: T) => U): Result<U, E> {
  if (result.ok) {
    return ok(fn(result.data));
  }
  return result;
}

/**
 * Map the error value of a result
 */
export function mapErr<T, E, F>(result: Result<T, E>, fn: (error: E) => F): Result<T, F> {
  if (result.ok) {
    return result;
  }
  return err(fn(result.error));
}

/**
 * Chain results together (flatMap)
 */
export function flatMap<T, U, E>(result: Result<T, E>, fn: (value: T) => Result<U, E>): Result<U, E> {
  if (result.ok) {
    return fn(result.data);
  }
  return result;
}

/**
 * Match on a result with handlers for success and error cases
 */
export function match<T, E, R>(
  result: Result<T, E>,
  handlers: {
    ok: (data: T) => R;
    err: (error: E) => R;
  },
): R {
  if (result.ok) {
    return handlers.ok(result.data);
  }
  return handlers.err(result.error);
}

/**
 * Convert a Promise to a Result
 */
export async function fromPromise<T, E = Error>(promise: Promise<T>, errorMapper?: (error: unknown) => E): Promise<Result<T, E>> {
  try {
    const data = await promise;
    return ok(data);
  } catch (error) {
    if (errorMapper) {
      return err(errorMapper(error));
    }
    return err(error as E);
  }
}

/**
 * Convert a Result to a Promise (throws on error)
 */
export function toPromise<T, E>(result: Result<T, E>): Promise<T> {
  if (result.ok) {
    return Promise.resolve(result.data);
  }
  return Promise.reject(result.error);
}
