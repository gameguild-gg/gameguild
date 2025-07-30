import { useCallback, useEffect, useState } from 'react';
import { useSync } from '../sync-provider';

export interface ContextIntegrationOptions {
  namespace?: string;
  syncKeys?: string[];
  autoSync?: boolean;
  syncInterval?: number;
}

export interface UseContextIntegrationResult<T> {
  // Data synchronization
  syncStateToStorage: (key: string, state: T) => Promise<void>;
  loadStateFromStorage: (key: string, defaultValue?: T) => Promise<T | null>;

  // Bulk operations
  syncMultipleStates: (states: Map<string, T>) => Promise<Map<string, boolean>>;
  loadMultipleStates: (keys: string[]) => Promise<Map<string, T>>;

  // State observation
  watchStateChanges: (key: string, callback: (newState: T) => void) => () => void;

  // Conflict resolution
  resolveStateConflict: (key: string, localState: T, remoteState: T) => Promise<T>;

  // Utilities
  clearNamespaceData: () => Promise<void>;
  getNamespaceKeys: () => Promise<string[]>;

  // Status
  isReady: boolean;
  lastSyncTime: Date | null;
  syncErrors: Error[];
}

/**
 * Hook for integrating storage with other React contexts
 */
export function useContextIntegration<T = unknown>(options: ContextIntegrationOptions = {}): UseContextIntegrationResult<T> {
  const { namespace = 'context', syncKeys = [], autoSync = false, syncInterval = 5000 } = options;

  const sync = useSync();
  const [isReady, setIsReady] = useState(false);
  const [lastSyncTime, setLastSyncTime] = useState<Date | null>(null);
  const [syncErrors, setSyncErrors] = useState<Error[]>([]);

  // Create namespaced key
  const createKey = useCallback((key: string) => `${namespace}:${key}`, [namespace]);

  // Initialize
  useEffect(() => {
    setIsReady(true);
  }, []);

  // Auto sync mechanism
  useEffect(() => {
    if (!autoSync || syncKeys.length === 0) return;

    const interval = setInterval(async () => {
      try {
        // Here you could implement auto-sync logic
        // For now, we just update the last sync time
        setLastSyncTime(new Date());
      } catch (error) {
        const err = error instanceof Error ? error : new Error('Auto sync failed');
        setSyncErrors((prev) => [...prev.slice(-9), err]); // Keep last 10 errors
      }
    }, syncInterval);

    return () => clearInterval(interval);
  }, [autoSync, syncKeys, syncInterval]);

  // Sync state to storage
  const syncStateToStorage = useCallback(
    async (key: string, state: T): Promise<void> => {
      const storageKey = createKey(key);
      const success = await sync.set(storageKey, state);

      if (!success) {
        throw new Error(`Failed to sync state for key: ${key}`);
      }

      setLastSyncTime(new Date());
    },
    [sync, createKey],
  );

  // Load state from storage
  const loadStateFromStorage = useCallback(
    async (key: string, defaultValue?: T): Promise<T | null> => {
      const storageKey = createKey(key);
      const state = await sync.get<T>(storageKey);
      return state ?? defaultValue ?? null;
    },
    [sync, createKey],
  );

  // Sync multiple states
  const syncMultipleStates = useCallback(
    async (states: Map<string, T>): Promise<Map<string, boolean>> => {
      const namespacedStates = new Map<string, T>();

      states.forEach((value, key) => {
        namespacedStates.set(createKey(key), value);
      });

      const results = await sync.setMany(namespacedStates);

      // Convert back to original keys
      const originalResults = new Map<string, boolean>();
      results.forEach((success, namespacedKey) => {
        const originalKey = namespacedKey.replace(`${namespace}:`, '');
        originalResults.set(originalKey, success);
      });

      setLastSyncTime(new Date());
      return originalResults;
    },
    [sync, createKey, namespace],
  );

  // Load multiple states
  const loadMultipleStates = useCallback(
    async (keys: string[]): Promise<Map<string, T>> => {
      const namespacedKeys = keys.map(createKey);
      const results = await sync.getMany<T>(namespacedKeys);

      // Convert back to original keys
      const originalResults = new Map<string, T>();
      results.forEach((value, namespacedKey) => {
        const originalKey = namespacedKey.replace(`${namespace}:`, '');
        originalResults.set(originalKey, value);
      });

      return originalResults;
    },
    [sync, createKey, namespace],
  );

  // Watch state changes
  const watchStateChanges = useCallback(
    (key: string, callback: (newState: T) => void): (() => void) => {
      const storageKey = createKey(key);

      return sync.onContentChange((event) => {
        if (event.key === storageKey && event.type === 'set') {
          callback(event.value as T);
        }
      });
    },
    [sync, createKey],
  );

  // Resolve state conflicts (simple last-write-wins for now)
  const resolveStateConflict = useCallback(
    async (key: string, localState: T, remoteState: T): Promise<T> => {
      // Simple conflict resolution - could be enhanced with more sophisticated logic
      const resolved = remoteState; // For now, prefer remote state
      await syncStateToStorage(key, resolved);
      return resolved;
    },
    [syncStateToStorage],
  );

  // Clear namespace data
  const clearNamespaceData = useCallback(async (): Promise<void> => {
    const allKeys = await sync.keys();
    const namespacePrefix = `${namespace}:`;
    const namespacedKeys = allKeys.filter((key) => key.startsWith(namespacePrefix)).map((key) => key.replace(namespacePrefix, ''));

    if (namespacedKeys.length > 0) {
      await sync.deleteMany(namespacedKeys.map(createKey));
    }
  }, [sync, namespace, createKey]);

  // Get namespace keys
  const getNamespaceKeys = useCallback(async (): Promise<string[]> => {
    const allKeys = await sync.keys();
    const namespacePrefix = `${namespace}:`;

    return allKeys.filter((key) => key.startsWith(namespacePrefix)).map((key) => key.replace(namespacePrefix, ''));
  }, [sync, namespace]);

  return {
    syncStateToStorage,
    loadStateFromStorage,
    syncMultipleStates,
    loadMultipleStates,
    watchStateChanges,
    resolveStateConflict,
    clearNamespaceData,
    getNamespaceKeys,
    isReady,
    lastSyncTime,
    syncErrors,
  };
}
