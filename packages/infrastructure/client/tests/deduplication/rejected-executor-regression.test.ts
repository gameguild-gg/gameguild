import { describe, expect, it } from 'vitest';
import { RequestDeduplicator } from '../../src/runtime/deduplication/deduplicator.js';
import { ok } from '../../src/runtime/result/helpers.js';
import type { Result } from '../../src/runtime/result/types.js';
import type { ApiError } from '../../src/runtime/errors/types.js';

describe('Request deduplication rejection cleanup', () => {
  it('releases rejected executors without an unhandled cleanup rejection', async () => {
    const deduplicator = new RequestDeduplicator({ enabled: true });
    const failure = new Error('Executor transport failed');
    let attempts = 0;
    const executor = (): Promise<Result<string, ApiError>> => {
      attempts++;
      return Promise.reject(failure);
    };

    const first = deduplicator.deduplicate('GET', '/api/rejected', undefined, executor);
    const second = deduplicator.deduplicate('GET', '/api/rejected', undefined, executor);
    const results = await Promise.allSettled([first, second]);

    expect(attempts).toBe(1);
    expect(results).toEqual([
      { status: 'rejected', reason: failure },
      { status: 'rejected', reason: failure },
    ]);
    expect(deduplicator.pendingCount).toBe(0);
    // Let Node report any orphaned promise created by cleanup before retrying.
    await new Promise<void>((resolve) => setTimeout(resolve, 0));

    const retried = await deduplicator.deduplicate('GET', '/api/rejected', undefined, () => Promise.resolve(ok('retried')));
    expect(retried).toEqual(ok('retried'));
    expect(deduplicator.pendingCount).toBe(0);
  });
});
