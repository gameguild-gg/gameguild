import { useCallback, useEffect, useRef } from 'react';
import { Content } from '@/types';
import { StorageStats } from '@/lib/tree/sync.types';

// Ultra-fast memory cache implementation
class UltraFastMemoryCache {
  private idToContent: Map<string, Content> = new Map();
  private slugToContent: Map<string, Content> = new Map();
  private dirtyChanges: Map<string, Partial<Content>> = new Map();

  constructor(content: Content[]) {
    this.rebuild(content);
  }

  rebuild(content: Content[]) {
    this.idToContent.clear();
    this.slugToContent.clear();
    // Keep dirty changes - don't clear them on rebuild

    content.forEach((item) => {
      if (item.id) {
        this.idToContent.set(item.id.toString(), item);
      }
      this.slugToContent.set(item.slug, item);
    });
  }

  // O(1): Instant memory-only override
  setDirtyChange(slug: string, change: Partial<Content>) {
    const existing = this.dirtyChanges.get(slug) || {};
    this.dirtyChanges.set(slug, { ...existing, ...change });
  }

  // O(1): Map lookup
  findById(id: string): Content | undefined {
    const baseContent = this.idToContent.get(id);
    if (!baseContent) return undefined;

    const change = this.dirtyChanges.get(baseContent.slug);
    return change ? { ...baseContent, ...change } : baseContent;
  }

  // O(1): Map lookup
  findBySlug(slug: string): Content | undefined {
    const baseContent = this.slugToContent.get(slug);
    if (!baseContent) return undefined;

    const change = this.dirtyChanges.get(slug);
    return change ? { ...baseContent, ...change } : baseContent;
  }

  // O(1): Get dirty changes
  getDirtyChanges(): Map<string, Partial<Content>> {
    return new Map(this.dirtyChanges);
  }

  // O(1): Clear specific changes
  clearSynced(slugs: string[]) {
    slugs.forEach((slug) => this.dirtyChanges.delete(slug));
  }

  // O(n): Only called during render, not during user interaction
  getAllWithChanges(): Content[] {
    const result: Content[] = [];
    this.slugToContent.forEach((baseContent) => {
      const change = this.dirtyChanges.get(baseContent.slug);
      result.push(change ? { ...baseContent, ...change } : baseContent);
    });
    return result;
  }

  size(): number {
    return this.idToContent.size;
  }

  getDirtyCount(): number {
    return this.dirtyChanges.size;
  }

  clear() {
    this.idToContent.clear();
    this.slugToContent.clear();
    this.dirtyChanges.clear();
  }
}

// Non-blocking streaming storage for localStorage
class NonBlockingStreamingStorage {
  private changeQueue: Array<{ slug: string; data: any; timestamp: number }> = [];
  private processTimeoutId: NodeJS.Timeout | null = null;
  private isProcessing = false;

  // O(1): Add to queue without blocking
  queueChange(slug: string, data: any): void {
    this.changeQueue.push({ slug, data, timestamp: Date.now() });
    this.scheduleProcessing();
  }

  // O(1): Get current state without iteration
  getAllPendingChanges(): Map<string, any> {
    const result = new Map<string, any>();

    try {
      // O(n) but only called during sync, not during user interaction
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key && key.startsWith('sync_change_')) {
          const slug = key.replace('sync_change_', '');
          const stored = localStorage.getItem(key);
          if (stored) {
            const parsed = JSON.parse(stored);
            result.set(slug, parsed.data);
          }
        }
      }
    } catch (error) {
      console.warn('Failed to read localStorage changes:', error);
    }

    return result;
  }

  // O(1): Clear specific keys
  clearProcessedChanges(slugs: string[]): void {
    // Use idle callback to avoid blocking
    const clearChunk = () => {
      const CHUNK_SIZE = 5;
      const toProcess = slugs.splice(0, CHUNK_SIZE);

      toProcess.forEach((slug) => {
        try {
          localStorage.removeItem(`sync_change_${slug}`);
        } catch (error) {
          console.warn('Failed to clear localStorage:', error);
        }
      });

      if (slugs.length > 0) {
        if (typeof requestIdleCallback !== 'undefined') {
          requestIdleCallback(clearChunk);
        } else {
          setTimeout(clearChunk, 0);
        }
      }
    };

    if (typeof requestIdleCallback !== 'undefined') {
      requestIdleCallback(clearChunk);
    } else {
      setTimeout(clearChunk, 0);
    }
  }

  getStorageStats(): { queueSize: number; localStorageCount: number } {
    let count = 0;
    try {
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key && key.startsWith('sync_change_')) count++;
      }
    } catch (error) {
      // Ignore errors in stats
    }

    return {
      queueSize: this.changeQueue.length,
      localStorageCount: count,
    };
  }

  clear() {
    this.changeQueue = [];
    // Clear all sync-related localStorage items
    const keysToRemove: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith('sync_change_')) {
        keysToRemove.push(key);
      }
    }
    keysToRemove.forEach((key) => localStorage.removeItem(key));
  }

  // Non-blocking: Process queue when main thread is idle
  private scheduleProcessing(): void {
    if (this.isProcessing || this.processTimeoutId) return;

    this.processTimeoutId = setTimeout(() => {
      this.processQueueInIdle();
    }, 0);
  }

  private processQueueInIdle(): void {
    this.processTimeoutId = null;

    if (this.changeQueue.length === 0) return;

    this.isProcessing = true;

    // Use requestIdleCallback for non-blocking localStorage operations
    const processChunk = (deadline?: IdleDeadline) => {
      const CHUNK_SIZE = 10; // Process in small chunks
      let processed = 0;

      while (this.changeQueue.length > 0 && processed < CHUNK_SIZE && (!deadline || deadline.timeRemaining() > 1)) {
        const change = this.changeQueue.shift()!;

        try {
          // O(1): Direct key-value store
          const key = `sync_change_${change.slug}`;
          localStorage.setItem(
            key,
            JSON.stringify({
              data: change.data,
              timestamp: change.timestamp,
            }),
          );
        } catch (error) {
          console.warn('Non-blocking localStorage failed:', error);
        }

        processed++;
      }

      // If more work to do, schedule next chunk
      if (this.changeQueue.length > 0) {
        if (typeof requestIdleCallback !== 'undefined') {
          requestIdleCallback(processChunk);
        } else {
          setTimeout(() => processChunk(), 0);
        }
      } else {
        this.isProcessing = false;
      }
    };

    // Start processing with idle callback or fallback
    if (typeof requestIdleCallback !== 'undefined') {
      requestIdleCallback(processChunk);
    } else {
      setTimeout(() => processChunk(), 0);
    }
  }
}

export const useCacheManager = (currentContent: Content[], maxCacheSize: number = 1000) => {
  const memoryCache = useRef<UltraFastMemoryCache>(new UltraFastMemoryCache(currentContent));
  const streamingStorage = useRef<NonBlockingStreamingStorage>(new NonBlockingStreamingStorage());
  const lastSyncTimeRef = useRef<number>(Date.now());

  // Initial cleanup on mount
  useEffect(() => {
    const initialCleanup = () => {
      if (currentContent.length === 0) return;

      const currentSlugs = new Set(currentContent.map((content) => content.slug));
      const staleEntries: string[] = [];

      try {
        for (let i = 0; i < localStorage.length; i++) {
          const key = localStorage.key(i);
          if (key && key.startsWith('sync_change_')) {
            const slug = key.replace('sync_change_', '');
            if (!currentSlugs.has(slug)) {
              staleEntries.push(slug);
            }
          }
        }

        if (staleEntries.length > 0) {
          streamingStorage.current.clearProcessedChanges([...staleEntries]);
          console.log(`Initial cleanup: removed ${staleEntries.length} stale localStorage entries`);
        }
      } catch (error) {
        console.warn('Initial localStorage cleanup failed:', error);
      }
    };

    if (currentContent.length > 0) {
      const cleanupTimeout = setTimeout(initialCleanup, 500);
      return () => clearTimeout(cleanupTimeout);
    }
  }, [currentContent.length > 0]);

  // Rebuild when content changes
  useEffect(() => {
    memoryCache.current.rebuild(currentContent);

    // Cleanup stale entries
    const cleanupStaleEntries = () => {
      const currentSlugs = new Set(currentContent.map((content) => content.slug));
      const staleEntries: string[] = [];

      try {
        for (let i = 0; i < localStorage.length; i++) {
          const key = localStorage.key(i);
          if (key && key.startsWith('sync_change_')) {
            const slug = key.replace('sync_change_', '');
            if (!currentSlugs.has(slug)) {
              staleEntries.push(slug);
            }
          }
        }

        if (staleEntries.length > 0) {
          streamingStorage.current.clearProcessedChanges([...staleEntries]);
        }
      } catch (error) {
        console.warn('Failed to cleanup stale localStorage entries:', error);
      }
    };

    const cleanupTimeout = setTimeout(cleanupStaleEntries, 100);
    return () => clearTimeout(cleanupTimeout);
  }, [currentContent]);

  // O(1): Memory-only operations
  const findContentById = useCallback((id: string): Content | undefined => {
    return memoryCache.current.findById(id);
  }, []);

  const findContentBySlug = useCallback((slug: string): Content | undefined => {
    return memoryCache.current.findBySlug(slug);
  }, []);

  // O(1): Instant memory + non-blocking localStorage
  const setDirtyChange = useCallback(
    (slug: string, change: any) => {
      // Validate slug format
      if (slug.includes('_') && /\d{13}$/.test(slug)) {
        const cleanSlug = slug.replace(/_\d{13}$/, '');
        const contentExists = currentContent.some((content) => content.slug === cleanSlug);
        if (contentExists) {
          slug = cleanSlug;
        } else {
          console.error(`Invalid slug: ${slug}`);
          return;
        }
      }

      // INSTANT: Memory update (O(1))
      memoryCache.current.setDirtyChange(slug, change);
      // NON-BLOCKING: Queue for localStorage (O(1))
      streamingStorage.current.queueChange(slug, change);
    },
    [currentContent],
  );

  // Get all content with memory changes applied
  const getAllContentWithChanges = useCallback((): Content[] => {
    return memoryCache.current.getAllWithChanges();
  }, []);

  // Get dirty changes
  const getDirtyChanges = useCallback((): Map<string, Partial<Content>> => {
    return memoryCache.current.getDirtyChanges();
  }, []);

  // Clear specific dirty changes
  const clearDirtyChanges = useCallback((contentIds: string[]) => {
    const slugs = contentIds
      .map((id) => {
        const content = memoryCache.current.findById(id);
        return content?.slug || id;
      })
      .filter(Boolean);

    memoryCache.current.clearSynced(slugs);
    streamingStorage.current.clearProcessedChanges([...slugs]);
  }, []);

  // Get stats
  const getStats = useCallback((): StorageStats => {
    const storageStats = streamingStorage.current.getStorageStats();
    return {
      memorySize: memoryCache.current.size(),
      dirtyChanges: memoryCache.current.getDirtyCount(),
      lastSyncTime: lastSyncTimeRef.current,
      ...storageStats,
    };
  }, []);

  // Clear all cache
  const clearCache = useCallback(() => {
    memoryCache.current.clear();
    streamingStorage.current.clear();
    lastSyncTimeRef.current = Date.now();
  }, []);

  return {
    cache: memoryCache.current,
    cacheSize: memoryCache.current.size(),
    findContentById,
    findContentBySlug,
    setDirtyChange,
    getAllContentWithChanges,
    getDirtyChanges,
    clearDirtyChanges,
    clearCache,
    getStats,
  };
};
