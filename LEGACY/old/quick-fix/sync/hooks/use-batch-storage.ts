import { useCallback, useState } from 'react';
import { useSync } from '../sync-provider';

export interface BatchOperation<T = unknown> {
  type: 'set' | 'delete';
  key: string;
  value?: T;
  ttl?: number;
}

export interface UseBatchStorageResult {
  executeBatch: <T>(operations: BatchOperation<T>[]) => Promise<Map<string, boolean>>;
  isExecuting: boolean;
  lastResult: Map<string, boolean> | null;
  error: Error | null;
}

/**
 * Hook for batch storage operations
 */
export function useBatchStorage(): UseBatchStorageResult {
  const sync = useSync();
  const [isExecuting, setIsExecuting] = useState(false);
  const [lastResult, setLastResult] = useState<Map<string, boolean> | null>(null);
  const [error, setError] = useState<Error | null>(null);

  const executeBatch = useCallback(
    async <T>(operations: BatchOperation<T>[]): Promise<Map<string, boolean>> => {
      if (operations.length === 0) {
        return new Map();
      }

      setIsExecuting(true);
      setError(null);

      try {
        const results = new Map<string, boolean>();

        // Separate operations by type
        const setOperations = operations.filter((op) => op.type === 'set');
        const deleteOperations = operations.filter((op) => op.type === 'delete');

        // Execute set operations
        if (setOperations.length > 0) {
          const setMap = new Map<string, T>();
          const ttlMap = new Map<string, number>();

          setOperations.forEach((op) => {
            if (op.value !== undefined) {
              setMap.set(op.key, op.value);
              if (op.ttl) {
                ttlMap.set(op.key, op.ttl);
              }
            }
          });

          if (setMap.size > 0) {
            // For operations with different TTLs, we need to execute them separately
            const groupedByTtl = new Map<number | undefined, Map<string, T>>();

            setOperations.forEach((op) => {
              const ttl = op.ttl;
              if (!groupedByTtl.has(ttl)) {
                groupedByTtl.set(ttl, new Map());
              }
              if (op.value !== undefined) {
                groupedByTtl.get(ttl)!.set(op.key, op.value);
              }
            });

            for (const [ttl, items] of groupedByTtl) {
              const setResults = await sync.setMany(items, ttl ? { ttl } : undefined);
              setResults.forEach((success, key) => {
                results.set(key, success);
              });
            }
          }
        }

        // Execute delete operations
        if (deleteOperations.length > 0) {
          const deleteKeys = deleteOperations.map((op) => op.key);
          const deleteResults = await sync.deleteMany(deleteKeys);
          deleteResults.forEach((success, key) => {
            results.set(key, success);
          });
        }

        setLastResult(results);
        return results;
      } catch (err) {
        const error = err instanceof Error ? err : new Error('Batch operation failed');
        setError(error);
        throw error;
      } finally {
        setIsExecuting(false);
      }
    },
    [sync],
  );

  return {
    executeBatch,
    isExecuting,
    lastResult,
    error,
  };
}
