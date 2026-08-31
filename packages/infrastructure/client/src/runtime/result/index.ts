/**
 * Result Module
 *
 * Re-exports all result-related types and utilities.
 */

export type { Result, Ok, Err, ResultData, ResultError } from './types.js';
export { ok, err, isOk, isErr, unwrap, unwrapOr, unwrapOrElse, map, mapErr, flatMap, match, fromPromise, toPromise } from './helpers.js';
