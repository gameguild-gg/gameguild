import { useCallback, useEffect, useRef, useState } from 'react';
import { useSync } from '../sync-provider';

export interface UseEnhancedStorageOptions {
  key: string;
  defaultValue?: unknown;
  ttl?: number;
  syncToFallbacks?: boolean;
  debounceMs?: number;
}

export interface UseEnhancedStorageResult<T> {
  data: T | null;
  setData: (value: T) => Promise<void>;
  deleteData: () => Promise<void>;
  isLoading: boolean;
  error: Error | null;
  lastModified: Date | null;
  exists: boolean;
}

/**
 * Enhanced storage hook with advanced features
 */
export function useEnhancedStorage<T = unknown>(options: UseEnhancedStorageOptions): UseEnhancedStorageResult<T> {
  const { key, defaultValue = null, ttl, debounceMs = 300 } = options;

  const sync = useSync();

  const [data, setDataState] = useState<T | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [lastModified, setLastModified] = useState<Date | null>(null);
  const [exists, setExists] = useState(false);

  const debounceTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const isInitializedRef = useRef(false);

  // Load initial data
  const loadData = useCallback(async () => {
    if (!key) return;

    try {
      setIsLoading(true);
      setError(null);

      const [value, hasValue] = await Promise.all([sync.get<T>(key), sync.has(key)]);

      setDataState(value ?? (defaultValue as T));
      setExists(hasValue);
      setLastModified(hasValue ? new Date() : null);
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Failed to load data');
      setError(error);
      setDataState(defaultValue as T);
    } finally {
      setIsLoading(false);
    }
  }, [key, defaultValue, sync]);

  // Initialize data on mount
  useEffect(() => {
    if (!isInitializedRef.current) {
      loadData();
      isInitializedRef.current = true;
    }
  }, [loadData]);

  // Set data with debouncing
  const setData = useCallback(
    async (value: T) => {
      if (!key) throw new Error('Key is required');

      try {
        setError(null);
        setDataState(value);
        setLastModified(new Date());

        // Clear existing debounce timeout
        if (debounceTimeoutRef.current) {
          clearTimeout(debounceTimeoutRef.current);
        }

        // Debounce the actual storage operation
        debounceTimeoutRef.current = setTimeout(async () => {
          try {
            const success = await sync.set(key, value, { ttl });
            if (success) {
              setExists(true);
            } else {
              throw new Error('Failed to save data');
            }
          } catch (err) {
            const error = err instanceof Error ? err : new Error('Failed to save data');
            setError(error);
            // Revert optimistic update
            loadData();
          }
        }, debounceMs);
      } catch (err) {
        const error = err instanceof Error ? err : new Error('Failed to set data');
        setError(error);
        throw error;
      }
    },
    [key, ttl, sync, debounceMs, loadData],
  );

  // Delete data
  const deleteData = useCallback(async () => {
    if (!key) throw new Error('Key is required');

    try {
      setError(null);

      const success = await sync.delete(key);
      if (success) {
        setDataState(defaultValue as T);
        setExists(false);
        setLastModified(null);
      } else {
        throw new Error('Failed to delete data');
      }
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Failed to delete data');
      setError(error);
      throw error;
    }
  }, [key, defaultValue, sync]);

  // Listen to storage changes for this key
  useEffect(() => {
    if (!key) return;

    const unsubscribe = sync.onContentChange((event) => {
      if (event.key === key) {
        if (event.type === 'set') {
          setDataState(event.value as T);
          setExists(true);
          setLastModified(new Date());
        } else if (event.type === 'delete') {
          setDataState(defaultValue as T);
          setExists(false);
          setLastModified(null);
        } else if (event.type === 'clear') {
          setDataState(defaultValue as T);
          setExists(false);
          setLastModified(null);
        }
      }
    });

    return unsubscribe;
  }, [key, defaultValue, sync]);

  // Cleanup debounce timeout on unmount
  useEffect(() => {
    return () => {
      if (debounceTimeoutRef.current) {
        clearTimeout(debounceTimeoutRef.current);
      }
    };
  }, []);

  return {
    data,
    setData,
    deleteData,
    isLoading,
    error,
    lastModified,
    exists,
  };
}
