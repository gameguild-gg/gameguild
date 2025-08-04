import { useCallback, useEffect, useRef, useState } from 'react';
import { useSync } from '../sync-provider';

export interface StorageChangeEvent {
  type: 'set' | 'delete' | 'clear';
  key?: string;
  value?: unknown;
  timestamp: number;
  adapter?: string;
}

export interface UseStorageObserverOptions {
  keys?: string[] | RegExp; // Specific keys to watch or pattern
  includeValues?: boolean; // Whether to include values in change events
  debounceMs?: number; // Debounce change events
}

export interface UseStorageObserverResult {
  changes: StorageChangeEvent[];
  lastChange: StorageChangeEvent | null;
  clearHistory: () => void;
  subscribeToKey: (key: string) => () => void;
  unsubscribeFromKey: (key: string) => void;
}

/**
 * Hook for observing storage changes across multiple keys with filtering
 */
export function useStorageObserver(options: UseStorageObserverOptions = {}): UseStorageObserverResult {
  const { keys, includeValues = false, debounceMs = 100 } = options;
  const sync = useSync();

  const [changes, setChanges] = useState<StorageChangeEvent[]>([]);
  const [lastChange, setLastChange] = useState<StorageChangeEvent | null>(null);

  const debounceTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const subscribedKeysRef = useRef<Set<string>>(new Set());

  // Filter function to check if we should process this change
  const shouldProcessChange = useCallback(
    (event: { type: 'set' | 'delete' | 'clear'; key?: string }) => {
      if (!keys) return true; // Watch all changes if no filter

      if (Array.isArray(keys)) {
        return !event.key || keys.includes(event.key);
      }

      if (keys instanceof RegExp) {
        return !event.key || keys.test(event.key);
      }

      return true;
    },
    [keys],
  );

  // Process change events
  const processChange = useCallback(
    (event: { type: 'set' | 'delete' | 'clear'; key?: string; value?: unknown }) => {
      if (!shouldProcessChange(event)) return;

      const changeEvent: StorageChangeEvent = {
        type: event.type,
        key: event.key,
        value: includeValues ? event.value : undefined,
        timestamp: Date.now(),
      };

      // Clear existing debounce timeout
      if (debounceTimeoutRef.current) {
        clearTimeout(debounceTimeoutRef.current);
      }

      // Debounce the state update
      debounceTimeoutRef.current = setTimeout(() => {
        setChanges((prev) => {
          const newChanges = [...prev, changeEvent];
          // Keep only last 100 changes to prevent memory leaks
          return newChanges.slice(-100);
        });
        setLastChange(changeEvent);
      }, debounceMs);
    },
    [shouldProcessChange, includeValues, debounceMs],
  );

  // Subscribe to storage changes
  useEffect(() => {
    const unsubscribe = sync.onContentChange(processChange);
    return unsubscribe;
  }, [sync, processChange]);

  // Subscribe to specific key
  const subscribeToKey = useCallback((key: string) => {
    subscribedKeysRef.current.add(key);

    return () => {
      subscribedKeysRef.current.delete(key);
    };
  }, []);

  // Unsubscribe from specific key
  const unsubscribeFromKey = useCallback((key: string) => {
    subscribedKeysRef.current.delete(key);
  }, []);

  // Clear change history
  const clearHistory = useCallback(() => {
    setChanges([]);
    setLastChange(null);
  }, []);

  // Cleanup debounce timeout on unmount
  useEffect(() => {
    return () => {
      if (debounceTimeoutRef.current) {
        clearTimeout(debounceTimeoutRef.current);
      }
    };
  }, []);

  return {
    changes,
    lastChange,
    clearHistory,
    subscribeToKey,
    unsubscribeFromKey,
  };
}
