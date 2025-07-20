'use client';

import React, { createContext, FunctionComponent, PropsWithChildren, useCallback, useContext, useEffect, useRef, useState } from 'react';
import { Content } from '@/types';

import { ConflictResolution, SyncOperation, SyncStatus } from './sync.types';

import { useContent } from '@/components/content.context';
import { useOptimisticUpdates } from './hooks/use-optimistic-updates.hook';
import { useConflictResolver } from './hooks/use-conflict-resolver';
import { debounce } from '@/lib/sync/utils/debunce';
import { useSyncQueue } from '@/lib/sync/hooks/sync-queue';
import { useCacheManager } from '@/lib/sync/hooks/cache-manager';

interface SyncContextType {
  // Sync Status
  syncStatus: SyncStatus;
  isSyncing: boolean;
  lastSyncTime: Date | null;
  syncError: Error | null;

  // Queue Management
  pendingOperations: SyncOperation[];
  failedOperations: SyncOperation[];

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
  pendingConflicts: Array<{ local: Content; remote: Content }>;
  resolveConflict: (contentId: string, useLocal: boolean) => Promise<void>;
}

const SyncContext = createContext<SyncContextType | undefined>(undefined);

interface SyncProviderProps {
  // Sync Configuration
  syncInterval?: number; // in milliseconds
  maxRetries?: number;
  enableBackgroundSync?: boolean;
  enableOptimisticUpdates?: boolean;
  conflictResolution?: ConflictResolution;
  maxCacheSize?: number;
  debounceDelay?: number;
}

export const SyncProvider: FunctionComponent<PropsWithChildren<SyncProviderProps>> = ({
  children,
  syncInterval = 5000,
  maxRetries = 3,
  enableBackgroundSync = true,
  enableOptimisticUpdates: initialOptimistic = true,
  conflictResolution = 'local-first',
  maxCacheSize = 1000,
  debounceDelay = 1000,
}: PropsWithChildren<SyncProviderProps>): React.JSX.Element => {
  const { state: contentState, update, create, delete: deleteContent, fetchAll } = useContent();

  // State
  const [syncStatus, setSyncStatus] = useState<SyncStatus>('idle');
  const [isSyncing, setIsSyncing] = useState(false);
  const [lastSyncTime, setLastSyncTime] = useState<Date | null>(null);
  const [syncError, setSyncError] = useState<Error | null>(null);
  const [enableOptimisticUpdatesState, setEnableOptimisticUpdates] = useState(initialOptimistic);
  const [conflictResolutionStrategy, setConflictResolutionStrategy] = useState<ConflictResolution>(conflictResolution);

  // Refs for intervals and debouncing
  const syncIntervalRef = useRef<NodeJS.Timeout>();
  const debouncedSyncRef = useRef<() => void>();

  // Custom hooks for different aspects of syncing
  const { queue, addToQueue, removeFromQueue, clearQueue, getFailedOperations, markAsFailed, markAsCompleted } = useSyncQueue(maxRetries);

  const { applyOptimisticUpdate, rollbackOptimisticUpdate, getOptimisticContent, clearOptimisticUpdates } = useOptimisticUpdates(contentState.items);

  const { pendingConflicts, addConflict, resolveConflict: resolveConflictInternal, clearConflicts } = useConflictResolver();

  const {
    cache,
    cacheSize,
    setDirtyChange,
    getDirtyChanges,
    clearDirtyChanges,
    clearCache: clearCacheInternal,
  } = useCacheManager(contentState.items, maxCacheSize);

  // Create debounced sync function
  useEffect(() => {
    debouncedSyncRef.current = debounce(async () => {
      await performSync();
    }, debounceDelay);
  }, [debounceDelay]);

  // Remove server actions line - we'll use the functions directly

  // Sync implementation with server actions integration and batch optimization
  const performSync = useCallback(async () => {
    if (isSyncing) return;

    // Get dirty changes from cache manager
    const dirtyChanges = getDirtyChanges();

    if (dirtyChanges.size === 0 && queue.length === 0) return;

    setIsSyncing(true);
    setSyncStatus('syncing');
    setSyncError(null);

    try {
      const allResults: Array<{ success: boolean; operation?: any; error?: Error }> = [];

      // Process dirty changes in batches for O(n) performance
      if (dirtyChanges.size > 0) {
        const BATCH_SIZE = 50;
        const changes = Array.from(dirtyChanges.entries());

        for (let i = 0; i < changes.length; i += BATCH_SIZE) {
          const batch = changes.slice(i, i + BATCH_SIZE);
          const batchUpdates: Array<{ id: string; data: any }> = [];

          batch.forEach(([slug, changeData]) => {
            const content = contentState.items.find((c) => c.slug === slug);
            if (content && content.id) {
              batchUpdates.push({ id: content.id.toString(), data: changeData });
            }
          });

          if (batchUpdates.length > 0) {
            try {
              // Use batch update for better performance
              const batchResults = await Promise.all(
                batchUpdates.map(({ id, data }) =>
                  serverActions
                    .updateContent(id, data)
                    .then((result) => ({ success: result.success, id }))
                    .catch((error) => ({ success: false, id, error })),
                ),
              );

              allResults.push(...batchResults);
            } catch (error) {
              console.error('Batch update failed:', error);
            }
          }
        }
      }

      // Process queued operations with optimized batching
      if (queue.length > 0) {
        // Group operations by type for batch processing
        const grouped = queue.reduce(
          (acc, op) => {
            if (!acc[op.type]) acc[op.type] = [];
            acc[op.type].push(op);
            return acc;
          },
          {} as Record<string, typeof queue>,
        );

        // Process each type in batches
        for (const [type, operations] of Object.entries(grouped)) {
          const BATCH_SIZE = 20;

          for (let i = 0; i < operations.length; i += BATCH_SIZE) {
            const batch = operations.slice(i, i + BATCH_SIZE);

            const batchPromises = batch.map(async (operation) => {
              try {
                let result;

                switch (operation.type) {
                  case 'create':
                    result = await serverActions.createContent(operation.data);
                    break;
                  case 'update':
                    result = await serverActions.updateContent(operation.contentId!, operation.data);
                    break;
                  case 'delete':
                    result = await serverActions.deleteContent(operation.contentId!);
                    break;
                }

                if (result.success) {
                  markAsCompleted(operation.id);

                  // Clear optimistic update on success
                  if (enableOptimisticUpdatesState && operation.contentId) {
                    rollbackOptimisticUpdate(operation.contentId);
                  }

                  return { success: true, operation };
                } else {
                  throw result.error || new Error('Operation failed');
                }
              } catch (error) {
                markAsFailed(operation.id, error instanceof Error ? error : new Error('Unknown error'));

                // Handle conflict detection
                if (error instanceof Error && error.message.includes('conflict') && operation.contentId) {
                  const remoteResult = await serverActions.fetchContent(operation.contentId);
                  if (remoteResult.success && remoteResult.data) {
                    const localContent = getOptimisticContent(operation.contentId);
                    if (localContent) {
                      addConflict({ local: localContent, remote: remoteResult.data });
                    }
                  }
                }

                return { success: false, operation, error: error as Error };
              }
            });

            const batchResults = await Promise.allSettled(batchPromises);
            allResults.push(...batchResults.map((r) => (r.status === 'fulfilled' ? r.value : { success: false, error: r.reason })));
          }
        }
      }

      // Calculate sync status
      const successCount = allResults.filter((r) => r.success).length;
      const totalCount = allResults.length;

      if (successCount === totalCount) {
        setSyncStatus('success');
      } else if (successCount > 0) {
        setSyncStatus('partial');
      } else {
        setSyncStatus('error');
      }

      setLastSyncTime(new Date());

      // Clear successfully synced changes from cache
      const syncedIds: string[] = [];

      allResults.forEach((result) => {
        if (result.success && result.operation) {
          if (result.operation.contentId) {
            syncedIds.push(result.operation.contentId);
          } else if ((result as any).id) {
            syncedIds.push((result as any).id);
          }
        }
      });

      if (syncedIds.length > 0) {
        clearDirtyChanges(syncedIds);
      }

      // Refresh content after sync
      await fetchAll();
    } catch (error) {
      setSyncStatus('error');
      setSyncError(error instanceof Error ? error : new Error('Sync failed'));
    } finally {
      setIsSyncing(false);
    }
  }, [
    queue,
    isSyncing,
    contentState,
    enableOptimisticUpdatesState,
    markAsCompleted,
    markAsFailed,
    rollbackOptimisticUpdate,
    getOptimisticContent,
    addConflict,
    getDirtyChanges,
    clearDirtyChanges,
    fetchAll,
  ]);

  // Background sync
  useEffect(() => {
    if (!enableBackgroundSync) return;

    syncIntervalRef.current = setInterval(() => {
      performSync();
    }, syncInterval);

    return () => {
      if (syncIntervalRef.current) {
        clearInterval(syncIntervalRef.current);
      }
    };
  }, [enableBackgroundSync, syncInterval, performSync]);

  // Fetch remote content using server actions
  const fetchRemoteContent = async (contentId: string): Promise<Content | null> => {
    try {
      const result = await serverActions.fetchContent(contentId);
      if (result.success && result.data) {
        return result.data;
      }
    } catch (error) {
      console.error('Failed to fetch remote content:', error);
    }
    return null;
  };

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
    failed.forEach((op) => {
      addToQueue(op);
    });
    await performSync();
  }, [getFailedOperations, addToQueue, performSync]);

  const resolveConflict = useCallback(
    async (contentId: string, useLocal: boolean) => {
      const conflict = pendingConflicts.find((c) => c.local.id === contentId);
      if (!conflict) return;

      if (useLocal) {
        // Keep local version
        await update(contentId, conflict.local);
      } else {
        // Use remote version
        rollbackOptimisticUpdate(contentId);
      }

      resolveConflictInternal(contentId);
    },
    [pendingConflicts, update, rollbackOptimisticUpdate, resolveConflictInternal],
  );

  const clearCache = useCallback(() => {
    clearCacheInternal();
    clearOptimisticUpdates();
    clearConflicts();
  }, [clearCacheInternal, clearOptimisticUpdates, clearConflicts]);

  const value: SyncContextType = {
    syncStatus,
    isSyncing,
    lastSyncTime,
    syncError,
    pendingOperations: queue,
    failedOperations: getFailedOperations(),
    forceSyncNow,
    clearSyncQueue,
    retryFailedOperations,
    enableOptimisticUpdates: enableOptimisticUpdatesState,
    setEnableOptimisticUpdates,
    cacheSize,
    clearCache,
    conflictResolutionStrategy,
    setConflictResolutionStrategy,
    pendingConflicts,
    resolveConflict,
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
