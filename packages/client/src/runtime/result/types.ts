/**
 * Result Type
 *
 * A discriminated union for handling success/failure states in a type-safe manner.
 */

/**
 * Success result
 */
export interface Ok<T> {
  readonly ok: true;
  readonly data: T;
  readonly error?: never;
}

/**
 * Error result
 */
export interface Err<E> {
  readonly ok: false;
  readonly data?: never;
  readonly error: E;
}

/**
 * Result type - either success with data or failure with error
 */
export type Result<T, E = Error> = Ok<T> | Err<E>;

/**
 * Extract the success type from a Result
 */
export type ResultData<R> = R extends Result<infer T, unknown> ? T : never;

/**
 * Extract the error type from a Result
 */
export type ResultError<R> = R extends Result<unknown, infer E> ? E : never;
