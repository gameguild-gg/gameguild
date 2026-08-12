// Runtime invariants for the ToolResult contract.
//
// The interface is documented in src/types.ts. This module provides a small
// runtime check helper that adapters (and tests) can use to fail fast when a
// returned value violates the contract. It is pure and DOM-free.

import { EmceptionError } from '../errors.js';
import type { ToolResult } from '../types.js';

/**
 * Validate that `value` is a well-formed {@link ToolResult}.
 *
 * Throws {@link EmceptionError} when an invariant is broken. Returns the
 * value (typed) on success so callers can chain:
 *
 * ```ts
 * return assertToolResult(await adapter.run(...));
 * ```
 *
 * @param value - candidate result from a tool invocation
 * @param context - optional label included in error messages (e.g. tool name)
 */
export function assertToolResult(value: unknown, context?: string): ToolResult {
  const where = context ? ` (${context})` : '';
  if (value === null || typeof value !== 'object') {
    throw new EmceptionError(`ToolResult must be an object${where}`);
  }
  const r = value as Record<string, unknown>;
  if (!Number.isFinite(r.exitCode)) {
    throw new EmceptionError(`ToolResult.exitCode must be a finite number${where}`);
  }
  if (typeof r.stdout !== 'string') {
    throw new EmceptionError(`ToolResult.stdout must be a string${where}`);
  }
  if (typeof r.stderr !== 'string') {
    throw new EmceptionError(`ToolResult.stderr must be a string${where}`);
  }
  if (typeof r.durationMs !== 'number' || !Number.isFinite(r.durationMs) || r.durationMs < 0) {
    throw new EmceptionError(`ToolResult.durationMs must be a non-negative finite number${where}`);
  }
  if (typeof r.timedOut !== 'boolean') {
    throw new EmceptionError(`ToolResult.timedOut must be a boolean${where}`);
  }
  if (r.signal !== undefined && typeof r.signal !== 'string') {
    throw new EmceptionError(`ToolResult.signal, when present, must be a string${where}`);
  }
  return value as ToolResult;
}

/**
 * Non-throwing predicate counterpart to {@link assertToolResult}.
 */
export function isToolResult(value: unknown): value is ToolResult {
  try {
    assertToolResult(value);
    return true;
  } catch {
    return false;
  }
}
