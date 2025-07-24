import { EventEmitter } from 'events';
import { StorageAdapter, StorageEvent, StorageEventHandler, StorageIndex, StorageMetadata, StorageOptions, StorageResult } from './storage-types';

// Base abstract storage adapter with optimized operations
export abstract class BaseStorageAdapter<T = unknown> extends EventEmitter implements StorageAdapter<T> {
  protected options: StorageOptions;
  protected index: StorageIndex<T>;

  // Performance optimization: Use WeakMap for caching
  private cache: WeakMap<object, T> = new WeakMap();
  private metadataCache: Map<string, StorageMetadata> = new Map();

  protected constructor(options: StorageOptions = {}) {
    super();
    this.options = options;
    this.index = this.createIndex();
  }

  // O(1) - Direct map lookup
  async exists(id: string): Promise<boolean> {
    return this.index.byId.has(id);
  }

  // O(1) - Direct map lookup with cache
  async getMetadata(id: string): Promise<StorageMetadata | null> {
    // Check cache first
    if (this.metadataCache.has(id)) {
      return this.metadataCache.get(id)!;
    }

    const metadata = this.index.metadata.get(id) || null;
    if (metadata) {
      this.metadataCache.set(id, metadata);
    }

    return metadata;
  }

  // O(1) - Direct map lookup
  async read(id: string): Promise<T | null> {
    const item = this.index.byId.get(id);
    return item || null;
  }

  // O(1) - Direct map insertion with index updates
  async write(id: string, data: T, metadata?: Partial<StorageMetadata>): Promise<StorageResult<T>> {
    try {
      const now = new Date().toISOString();
      const existingMetadata = await this.getMetadata(id);

      const fullMetadata: StorageMetadata = {
        id,
        type: metadata?.type || 'unknown',
        createdAt: existingMetadata?.createdAt || now,
        updatedAt: now,
        ...metadata,
      };

      // Update indices - all O(1) operations
      this.index.byId.set(id, data);
      this.index.metadata.set(id, fullMetadata);

      // Update type index
      if (fullMetadata.type) {
        if (!this.index.byType!.has(fullMetadata.type)) {
          this.index.byType!.set(fullMetadata.type, new Set());
        }
        this.index.byType!.get(fullMetadata.type)!.add(id);
      }

      // Update date index (by day for efficient range queries)
      const dateKey = now.split('T')[0]; // YYYY-MM-DD
      if (!this.index.byDate!.has(dateKey)) {
        this.index.byDate!.set(dateKey, new Set());
      }
      this.index.byDate!.get(dateKey)!.add(id);

      // Clear caches
      this.metadataCache.delete(id);

      // Emit event
      this.emitStorageEvent({
        type: existingMetadata ? 'update' : 'create',
        id,
        data,
        metadata: fullMetadata,
        timestamp: Date.now(),
      });

      // Persist to storage (implemented by subclasses)
      await this.persistWrite(id, data, fullMetadata);

      return {
        success: true,
        data,
        metadata: fullMetadata,
      };
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error : new Error('Write failed'),
      };
    }
  }

  // O(1) - Direct map deletion
  async delete(id: string): Promise<boolean> {
    const metadata = await this.getMetadata(id);
    if (!metadata) return false;

    // Remove from all indices - all O(1) operations
    this.index.byId.delete(id);
    this.index.metadata.delete(id);

    if (metadata.type && this.index.byType!.has(metadata.type)) {
      this.index.byType!.get(metadata.type)!.delete(id);
    }

    const dateKey = metadata.createdAt.split('T')[0];
    if (this.index.byDate!.has(dateKey)) {
      this.index.byDate!.get(dateKey)!.delete(id);
    }

    // Clear caches
    this.metadataCache.delete(id);

    // Emit event
    this.emitStorageEvent({
      type: 'delete',
      id,
      metadata,
      timestamp: Date.now(),
    });

    // Persist deletion (implemented by subclasses)
    await this.persistDelete(id);

    return true;
  }

  // O(n) but optimized with indices and pagination
  async list(prefix?: string, limit: number = 100, offset: number = 0): Promise<StorageMetadata[]> {
    const allMetadata = Array.from(this.index.metadata.values());

    // Filter by prefix if provided
    const filtered = prefix ? allMetadata.filter((m) => m.id.startsWith(prefix)) : allMetadata;

    // Sort by updatedAt desc for recent items first
    filtered.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime());

    // Apply pagination
    return filtered.slice(offset, offset + limit);
  }

  // Batch operations - optimized for performance
  async batchRead(ids: string[]): Promise<Map<string, T>> {
    const result = new Map<string, T>();

    // Use Promise.all for parallel reads
    const promises = ids.map(async (id) => {
      const data = await this.read(id);
      if (data) {
        result.set(id, data);
      }
    });

    await Promise.all(promises);
    return result;
  }

  async batchWrite(items: Map<string, T>): Promise<Map<string, StorageResult<T>>> {
    const results = new Map<string, StorageResult<T>>();

    // Process in chunks to avoid overwhelming the system
    const CHUNK_SIZE = 50;
    const entries = Array.from(items.entries());

    for (let i = 0; i < entries.length; i += CHUNK_SIZE) {
      const chunk = entries.slice(i, i + CHUNK_SIZE);

      const chunkPromises = chunk.map(async ([id, data]) => {
        const result = await this.write(id, data);
        results.set(id, result);
      });

      await Promise.all(chunkPromises);
    }

    return results;
  }

  async batchDelete(ids: string[]): Promise<Map<string, boolean>> {
    const results = new Map<string, boolean>();

    // Process in chunks
    const CHUNK_SIZE = 100;

    for (let i = 0; i < ids.length; i += CHUNK_SIZE) {
      const chunk = ids.slice(i, i + CHUNK_SIZE);

      const chunkPromises = chunk.map(async (id) => {
        const result = await this.delete(id);
        results.set(id, result);
      });

      await Promise.all(chunkPromises);
    }

    return results;
  }

  // Event handling
  onStorageEvent(handler: StorageEventHandler<T>): void {
    this.on('storage-event', handler);
  }

  offStorageEvent(handler: StorageEventHandler<T>): void {
    this.off('storage-event', handler);
  }

  // O(n) but optimized with the date index
  async findByDateRange(start: Date, end: Date): Promise<string[]> {
    const dateKeys = this.getDateRange(start, end);
    const ids = new Set<string>();

    for (const dateKey of dateKeys) {
      const dateIds = this.index.byDate!.get(dateKey);
      if (dateIds) {
        dateIds.forEach((id) => ids.add(id));
      }
    }

    return Array.from(ids);
  }

  // Create optimized index structures - O(1) lookups
  protected createIndex(): StorageIndex<T> {
    return {
      byId: new Map(),
      bySlug: new Map(),
      byType: new Map(),
      byDate: new Map(),
      metadata: new Map(),
    };
  }

  protected emitStorageEvent(event: StorageEvent<T>): void {
    this.emit('storage-event', event);
  }

  // Abstract methods to be implemented by subclasses
  protected abstract persistWrite(id: string, data: T, metadata: StorageMetadata): Promise<void>;

  protected abstract persistDelete(id: string): Promise<void>;

  protected abstract loadIndex(): Promise<void>;

  // Utility methods for efficient operations
  protected getDateRange(start: Date, end: Date): string[] {
    const dates: string[] = [];
    const current = new Date(start);

    while (current <= end) {
      dates.push(current.toISOString().split('T')[0]);
      current.setDate(current.getDate() + 1);
    }

    return dates;
  }
}
