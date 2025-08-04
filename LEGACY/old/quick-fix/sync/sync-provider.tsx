'use client';

import React, { createContext, FunctionComponent, PropsWithChildren, useCallback, useContext, useEffect, useRef, useState } from 'react';

import { ConflictResolution, SyncOperation, SyncStatus } from './sync-types';
import { StorageManager, StorageManagerConfig } from './storage/storage-manager';
import { StorageAdapterType } from './storage/storage-types';
import { debounce } from '@/lib/sync/utils/debounce';

interface SyncContextType {
  // Storage Configuration
  storageAdapter: StorageAdapterType;
  setStorageAdapter: (adapter: StorageAdapterType) => Promise<void>;
  availableAdapters: StorageAdapterType[];

  // Sync Status
  syncStatus: SyncStatus;
  isSyncing: boolean;
  lastSyncTime: Date | null;
  syncError: Error | null;

  // Queue Management
  pendingOperations: SyncOperation[];
  failedOperations: SyncOperation[];

  // Storage Operations
  get: <T>(key: string) => Promise<T | null>;
  set: <T>(key: string, value: T, options?: { ttl?: number }) => Promise<boolean>;
  delete: (key: string) => Promise<boolean>;
  clear: () => Promise<boolean>;
  has: (key: string) => Promise<boolean>;
  keys: () => Promise<string[]>;

  // Batch Operations
  getMany: <T>(keys: string[]) => Promise<Map<string, T>>;
  setMany: <T>(items: Map<string, T>, options?: { ttl?: number }) => Promise<Map<string, boolean>>;
  deleteMany: (keys: string[]) => Promise<Map<string, boolean>>;

  // Sync Methods
  forceSyncNow: () => Promise<void>;
  clearSyncQueue: () => void;
  retryFailedOperations: () => Promise<void>;

  // Performance Features
  enableOptimisticUpdates: boolean;
  setEnableOptimisticUpdates: (enabled: boolean) => void;
  cacheSize: number;
  clearCache: () => void;

  // Conflict Resolution
  conflictResolutionStrategy: ConflictResolution;
  setConflictResolutionStrategy: (strategy: ConflictResolution) => void;
  pendingConflicts: Array<{ local: { id: string }; remote: { id: string } }>;
  resolveConflict: (contentId: string, useLocal: boolean) => Promise<void>;

  // Storage Statistics
  getStorageStats: () => Promise<any>;

  // Content Change Events
  onContentChange: (callback: (event: { type: 'set' | 'delete' | 'clear'; key?: string; value?: unknown }) => void) => () => void;
}

const SyncContext = createContext<SyncContextType | undefined>(undefined);

interface SyncProviderProps {
  // Storage Configuration
  primaryStorageAdapter?: StorageAdapterType;
  fallbackAdapters?: StorageAdapterType[];
  namespace?: string;

  // Sync Configuration
  syncInterval?: number; // in milliseconds
  maxRetries?: number;
  enableBackgroundSync?: boolean;
  enableOptimisticUpdates?: boolean;
  conflictResolution?: ConflictResolution;
  maxCacheSize?: number;
  debounceDelay?: number;
  defaultTTL?: number;
}

export const SyncProvider: FunctionComponent<PropsWithChildren<SyncProviderProps>> = ({
  children,
  primaryStorageAdapter = 'indexedDB',
  fallbackAdapters = ['localStorage', 'sessionStorage', 'memory'],
  namespace = 'app-sync',
  syncInterval = 5000,
  maxRetries = 3,
  enableBackgroundSync = true,
  enableOptimisticUpdates: initialOptimistic = true,
  conflictResolution = 'local-first',
  maxCacheSize: _maxCacheSize = 1000,
  debounceDelay = 1000,
  defaultTTL = 24 * 60 * 60 * 1000, // 24 hours
}: PropsWithChildren<SyncProviderProps>): React.JSX.Element => {
  // Storage Manager
  const [storageManager, setStorageManager] = useState<StorageManager | null>(null);
  const [currentAdapter, setCurrentAdapter] = useState<StorageAdapterType>(primaryStorageAdapter);
  const [availableAdapters, setAvailableAdapters] = useState<StorageAdapterType[]>([]);

  // State
  const [syncStatus, setSyncStatus] = useState<SyncStatus>('idle');
  const [isSyncing] = useState(false);
  const [lastSyncTime] = useState<Date | null>(null);
  const [syncError] = useState<Error | null>(null);
  const [enableOptimisticUpdatesState, setEnableOptimisticUpdates] = useState(initialOptimistic);
  const [conflictResolutionStrategy, setConflictResolutionStrategy] = useState<ConflictResolution>(conflictResolution);

  // Refs for intervals and debouncing
  const syncIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const debouncedSyncRef = useRef<(() => void) | null>(null);

  // Mock hook implementations
  const queue: SyncOperation[] = [];
  const addToQueue = (_op: SyncOperation) => {};
  const clearQueue = () => {};
  const getFailedOperations = () => [];

  const rollbackOptimisticUpdate = (_contentId: string) => {};
  const clearOptimisticUpdates = () => {};

  const pendingConflicts: Array<{ local: { id: string }; remote: { id: string } }> = [];
  const resolveConflictInternal = (_contentId: string) => Promise.resolve();
  const clearConflicts = () => {};

  const cacheSize = 0;
  const clearCacheInternal = () => {};

  // Initialize storage manager
  useEffect(() => {
    const initStorageManager = async () => {
      try {
        const config: StorageManagerConfig = {
          primaryAdapter: currentAdapter,
          fallbackAdapters,
          namespace,
          defaultTTL,
          autoMigrate: true,
          compressionThreshold: 1024,
          encryptionEnabled: false,
        };

        const manager = new StorageManager(config);
        await manager.init();

        // Get available adapters
        const { StorageAdapterFactory } = await import('./storage/storage-adapters');
        const available = StorageAdapterFactory.getAvailableTypes();
        setAvailableAdapters(available);

        setStorageManager(manager);
        setSyncStatus('idle');
      } catch (error) {
        console.error('Failed to initialize storage manager:', error);
        setSyncStatus('error');
      }
    };

    initStorageManager();

    return () => {
      if (storageManager) {
        storageManager.destroy().catch(console.error);
      }
    };
  }, [currentAdapter, fallbackAdapters, namespace, defaultTTL]);

  // Create debounced sync function
  useEffect(() => {
    debouncedSyncRef.current = debounce(async () => {
      await performSync();
    }, debounceDelay);
  }, [debounceDelay]);

  // Sync implementation
  const performSync = useCallback(async () => {
    // TODO: Implement proper sync functionality with server
    console.log('Sync functionality not yet implemented');
    return Promise.resolve();
  }, []);

  // Background sync
  useEffect(() => {
    if (!enableBackgroundSync || !storageManager) return;

    syncIntervalRef.current = setInterval(() => {
      performSync();
    }, syncInterval);

    return () => {
      if (syncIntervalRef.current) {
        clearInterval(syncIntervalRef.current);
      }
    };
  }, [enableBackgroundSync, syncInterval, performSync, storageManager]);

  // Storage operations
  const get = useCallback(
    async <T,>(key: string): Promise<T | null> => {
      if (!storageManager) throw new Error('Storage not initialized');
      return storageManager.get<T>(key);
    },
    [storageManager],
  );

  const set = useCallback(
    async <T,>(key: string, value: T, options?: { ttl?: number }): Promise<boolean> => {
      if (!storageManager) throw new Error('Storage not initialized');
      return storageManager.set(key, value, options);
    },
    [storageManager],
  );

  const deleteItem = useCallback(
    async (key: string): Promise<boolean> => {
      if (!storageManager) throw new Error('Storage not initialized');
      return storageManager.delete(key);
    },
    [storageManager],
  );

  const clear = useCallback(async (): Promise<boolean> => {
    if (!storageManager) throw new Error('Storage not initialized');
    return storageManager.clear();
  }, [storageManager]);

  const has = useCallback(
    async (key: string): Promise<boolean> => {
      if (!storageManager) throw new Error('Storage not initialized');
      return storageManager.has(key);
    },
    [storageManager],
  );

  const keys = useCallback(async (): Promise<string[]> => {
    if (!storageManager) throw new Error('Storage not initialized');
    return storageManager.keys();
  }, [storageManager]);

  // Batch operations
  const getMany = useCallback(
    async <T,>(keys: string[]): Promise<Map<string, T>> => {
      if (!storageManager) throw new Error('Storage not initialized');
      return storageManager.getMany<T>(keys);
    },
    [storageManager],
  );

  const setMany = useCallback(
    async <T,>(items: Map<string, T>, options?: { ttl?: number }): Promise<Map<string, boolean>> => {
      if (!storageManager) throw new Error('Storage not initialized');
      return storageManager.setMany(items, options);
    },
    [storageManager],
  );

  const deleteMany = useCallback(
    async (keys: string[]): Promise<Map<string, boolean>> => {
      if (!storageManager) throw new Error('Storage not initialized');
      return storageManager.deleteMany(keys);
    },
    [storageManager],
  );

  // Storage adapter switching
  const setStorageAdapter = useCallback(
    async (adapter: StorageAdapterType): Promise<void> => {
      if (currentAdapter === adapter || !storageManager) return;

      try {
        // Migrate data to new adapter if needed
        const oldKeys = await storageManager.keys();
        if (oldKeys.length > 0) {
          const data = await storageManager.getMany(oldKeys);

          // Destroy old manager
          await storageManager.destroy();

          // Create new manager with new adapter
          const config: StorageManagerConfig = {
            primaryAdapter: adapter,
            fallbackAdapters,
            namespace,
            defaultTTL,
            autoMigrate: true,
            compressionThreshold: 1024,
            encryptionEnabled: false,
          };

          const newManager = new StorageManager(config);
          await newManager.init();

          // Migrate data
          if (data.size > 0) {
            await newManager.setMany(data);
          }

          setStorageManager(newManager);
        }

        setCurrentAdapter(adapter);
      } catch (error) {
        console.error('Failed to switch storage adapter:', error);
        throw error;
      }
    },
    [currentAdapter, storageManager, fallbackAdapters, namespace, defaultTTL],
  );

  // Get storage statistics
  const getStorageStats = useCallback(async () => {
    if (!storageManager) return null;
    return storageManager.getStats();
  }, [storageManager]);

  // Content change events
  const onContentChange = useCallback(
    (callback: (event: { type: 'set' | 'delete' | 'clear'; key?: string; value?: unknown }) => void) => {
      if (!storageManager) return () => {};
      return storageManager.onChange(callback);
    },
    [storageManager],
  );

  // Public methods
  const forceSyncNow = useCallback(async () => {
    await performSync();
  }, [performSync]);

  const clearSyncQueue = useCallback(() => {
    clearQueue();
    clearOptimisticUpdates();
    setSyncStatus('idle');
  }, [clearQueue, clearOptimisticUpdates]);

  const retryFailedOperations = useCallback(async () => {
    const failed = getFailedOperations();
    failed.forEach((op: unknown) => {
      addToQueue(op);
    });
    await performSync();
  }, [getFailedOperations, addToQueue, performSync]);

  const resolveConflict = useCallback(
    async (contentId: string, useLocal: boolean) => {
      const conflict = pendingConflicts.find((c: { local: { id: string } }) => c.local.id === contentId);
      if (!conflict) return;

      if (useLocal) {
        // Keep local version
        await set(contentId, conflict.local);
      } else {
        // Use remote version
        rollbackOptimisticUpdate(contentId);
      }

      resolveConflictInternal(contentId);
    },
    [pendingConflicts, set, rollbackOptimisticUpdate, resolveConflictInternal],
  );

  const clearCache = useCallback(() => {
    clearCacheInternal();
    clearOptimisticUpdates();
    clearConflicts();
  }, [clearCacheInternal, clearOptimisticUpdates, clearConflicts]);

  const value: SyncContextType = {
    // Storage Configuration
    storageAdapter: currentAdapter,
    setStorageAdapter,
    availableAdapters,

    // Sync Status
    syncStatus,
    isSyncing,
    lastSyncTime,
    syncError,

    // Queue Management
    pendingOperations: queue,
    failedOperations: getFailedOperations(),

    // Storage Operations
    get,
    set,
    delete: deleteItem,
    clear,
    has,
    keys,

    // Batch Operations
    getMany,
    setMany,
    deleteMany,

    // Sync Methods
    forceSyncNow,
    clearSyncQueue,
    retryFailedOperations,

    // Performance Features
    enableOptimisticUpdates: enableOptimisticUpdatesState,
    setEnableOptimisticUpdates,
    cacheSize,
    clearCache,

    // Conflict Resolution
    conflictResolutionStrategy,
    setConflictResolutionStrategy,
    pendingConflicts,
    resolveConflict,

    // Storage Statistics
    getStorageStats,

    // Content Change Events
    onContentChange,
  };

  return <SyncContext.Provider value={value}>{children}</SyncContext.Provider>;
};

export const useSync = (): SyncContextType => {
  const context = useContext(SyncContext);
  if (context === undefined) {
    throw new Error('useSync must be used within a SyncProvider');
  }
  return context;
};
