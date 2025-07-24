import { useCallback } from 'react';
import { Content } from '@/types';
import { useContent } from '@/components/content.context';
import { useSync } from '../sync-provider';
import { useSyncQueue } from '@/lib/sync/hooks/sync-queue';
import { useCacheManager } from '@/lib/sync/hooks/cache-manager';

interface OptimizedContentHook {
  // Cache operations (O(1))
  findContentById: (id: string) => Content | undefined;
  findContentBySlug: (slug: string) => Content | undefined;
  getAllContentWithChanges: () => Content[];

  // Update operations
  setDirtyChange: (slug: string, change: Partial<Content>) => void;
  applyBatchChanges: (updates: Array<{ slug: string; data: Partial<Content> }>) => void;

  // Sync operations
  debouncedSync: () => void;
  forceSync: () => Promise<void>;

  // Stats
  getStats: () => {
    memorySize: number;
    dirtyChanges: number;
    lastSyncTime: number;
    queueSize: number;
    localStorageCount: number;
  };
}

export const useOptimizedContent = (): OptimizedContentHook => {
  const { state: contentState } = useContent();
  const { forceSyncNow } = useSync();

  const { addUpdateOperation, getQueueStats } = useSyncQueue();

  const currentContent = Object.values(contentState.items);

  const {
    findContentById,
    findContentBySlug,
    setDirtyChange: setCacheDirtyChange,
    getAllContentWithChanges,
    getStats: getCacheStats,
  } = useCacheManager(currentContent);

  // Enhanced setDirtyChange that integrates with sync queue
  const setDirtyChange = useCallback(
    (slug: string, change: Partial<Content>) => {
      // Apply to cache immediately
      setCacheDirtyChange(slug, change);

      // Find content to get ID
      const content = findContentBySlug(slug);
      if (content && content.id) {
        // Add to sync queue for background sync
        addUpdateOperation(content.id.toString(), change);
      }
    },
    [setCacheDirtyChange, findContentBySlug, addUpdateOperation],
  );

  // Batch apply changes
  const applyBatchChanges = useCallback(
    (updates: Array<{ slug: string; data: Partial<Content> }>) => {
      updates.forEach(({ slug, data }) => {
        setDirtyChange(slug, data);
      });
    },
    [setDirtyChange],
  );

  // Debounced sync (uses the sync provider's mechanism)
  const debouncedSync = useCallback(() => {
    // The sync provider handles debouncing internally
    // This is just a trigger
    forceSyncNow();
  }, [forceSyncNow]);

  // Force sync
  const forceSync = useCallback(async () => {
    await forceSyncNow();
  }, [forceSyncNow]);

  // Get combined stats
  const getStats = useCallback(() => {
    const cacheStats = getCacheStats();
    const queueStats = getQueueStats();

    return {
      memorySize: cacheStats.memorySize,
      dirtyChanges: cacheStats.dirtyChanges,
      lastSyncTime: cacheStats.lastSyncTime,
      queueSize: queueStats.total,
      localStorageCount: cacheStats.localStorageCount,
    };
  }, [getCacheStats, getQueueStats]);

  return {
    findContentById,
    findContentBySlug,
    getAllContentWithChanges,
    setDirtyChange,
    applyBatchChanges,
    debouncedSync,
    forceSync,
    getStats,
  };
};
