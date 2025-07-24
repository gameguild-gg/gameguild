import { EventEmitter } from 'events';
import { ClientStorageAdapter, StorageAdapterConfig, StorageAdapterType, StorageAdapterEvent } from './storage-types';
import { StorageAdapterFactory } from './storage-adapters';

export interface StorageManagerConfig {
  primaryAdapter: StorageAdapterType;
  fallbackAdapters?: StorageAdapterType[];
  namespace?: string;
  defaultTTL?: number;
  autoMigrate?: boolean;
  compressionThreshold?: number; // Bytes
  encryptionEnabled?: boolean;
}

export interface StorageManagerStats {
  primary: {
    type: StorageAdapterType;
    available: boolean;
    size: number;
    itemCount: number;
  };
  fallbacks: Array<{
    type: StorageAdapterType;
    available: boolean;
    size: number;
    itemCount: number;
  }>;
  totalSize: number;
  totalItems: number;
}

/**
 * Enhanced Storage Manager that orchestrates multiple client-side storage adapters
 */
export class StorageManager extends EventEmitter {
  private config: StorageManagerConfig;
  private primaryAdapter: ClientStorageAdapter | null = null;
  private fallbackAdapters: ClientStorageAdapter[] = [];
  private initialized = false;

  constructor(config: StorageManagerConfig) {
    super();
    this.config = {
      fallbackAdapters: ['memory'],
      namespace: 'app',
      defaultTTL: 24 * 60 * 60 * 1000, // 24 hours
      autoMigrate: true,
      compressionThreshold: 1024, // 1KB
      encryptionEnabled: false,
      ...config,
    };
  }

  async init(): Promise<void> {
    if (this.initialized) return;

    try {
      // Initialize primary adapter
      await this.initializePrimaryAdapter();

      // Initialize fallback adapters
      await this.initializeFallbackAdapters();

      // Auto-migrate if needed
      if (this.config.autoMigrate) {
        await this.migrateData();
      }

      this.initialized = true;
      this.emit('initialized');
    } catch (error) {
      this.emit('error', error);
      throw error;
    }
  }

  async destroy(): Promise<void> {
    if (this.primaryAdapter) {
      await this.primaryAdapter.destroy();
      this.primaryAdapter = null;
    }

    for (const adapter of this.fallbackAdapters) {
      await adapter.destroy();
    }
    this.fallbackAdapters = [];

    this.initialized = false;
    this.removeAllListeners();
  }

  // Core storage operations
  async get<T = unknown>(key: string): Promise<T | null> {
    this.ensureInitialized();

    const adapters = this.getAvailableAdapters();

    for (const adapter of adapters) {
      try {
        const result = await adapter.get<T>(key);
        if (result.success && result.data !== undefined) {
          // Propagate to other adapters if found in fallback
          if (adapter !== this.primaryAdapter && this.primaryAdapter) {
            this.setInBackground(key, result.data);
          }
          return result.data;
        }
      } catch (error) {
        this.emit('adapter-error', { adapter: adapter.type, error, operation: 'get', key });
        continue;
      }
    }

    return null;
  }

  async set<T = unknown>(key: string, value: T, options?: { ttl?: number; skipFallbacks?: boolean }): Promise<boolean> {
    this.ensureInitialized();

    const ttl = options?.ttl || this.config.defaultTTL;
    const adapters = options?.skipFallbacks ? [this.primaryAdapter].filter(Boolean) : this.getAvailableAdapters();

    let success = false;

    for (const adapter of adapters) {
      try {
        const result = await adapter.set(key, value, ttl);
        if (result.success) {
          success = true;
          this.emit('stored', { adapter: adapter.type, key, value });
        }
      } catch (error) {
        this.emit('adapter-error', { adapter: adapter.type, error, operation: 'set', key });
        continue;
      }
    }

    return success;
  }

  async delete(key: string): Promise<boolean> {
    this.ensureInitialized();

    const adapters = this.getAvailableAdapters();
    let deleted = false;

    for (const adapter of adapters) {
      try {
        const result = await adapter.delete(key);
        if (result.success && result.data) {
          deleted = true;
          this.emit('deleted', { adapter: adapter.type, key });
        }
      } catch (error) {
        this.emit('adapter-error', { adapter: adapter.type, error, operation: 'delete', key });
        continue;
      }
    }

    return deleted;
  }

  async clear(): Promise<boolean> {
    this.ensureInitialized();

    const adapters = this.getAvailableAdapters();
    let cleared = false;

    for (const adapter of adapters) {
      try {
        const result = await adapter.clear();
        if (result.success) {
          cleared = true;
          this.emit('cleared', { adapter: adapter.type });
        }
      } catch (error) {
        this.emit('adapter-error', { adapter: adapter.type, error, operation: 'clear' });
        continue;
      }
    }

    return cleared;
  }

  async has(key: string): Promise<boolean> {
    this.ensureInitialized();

    const adapters = this.getAvailableAdapters();

    for (const adapter of adapters) {
      try {
        const exists = await adapter.has(key);
        if (exists) {
          return true;
        }
      } catch (error) {
        this.emit('adapter-error', { adapter: adapter.type, error, operation: 'has', key });
        continue;
      }
    }

    return false;
  }

  async keys(): Promise<string[]> {
    this.ensureInitialized();

    const allKeys = new Set<string>();
    const adapters = this.getAvailableAdapters();

    for (const adapter of adapters) {
      try {
        const keys = await adapter.keys();
        keys.forEach((key) => allKeys.add(key));
      } catch (error) {
        this.emit('adapter-error', { adapter: adapter.type, error, operation: 'keys' });
        continue;
      }
    }

    return Array.from(allKeys);
  }

  // Batch operations
  async getMany<T = unknown>(keys: string[]): Promise<Map<string, T>> {
    this.ensureInitialized();

    const result = new Map<string, T>();
    const adapters = this.getAvailableAdapters();

    for (const adapter of adapters) {
      try {
        const adapterResult = await adapter.getMany<T>(keys);
        adapterResult.forEach((value, key) => {
          if (!result.has(key)) {
            result.set(key, value);
          }
        });
      } catch (error) {
        this.emit('adapter-error', { adapter: adapter.type, error, operation: 'getMany' });
        continue;
      }
    }

    return result;
  }

  async setMany<T = unknown>(items: Map<string, T>, options?: { ttl?: number; skipFallbacks?: boolean }): Promise<Map<string, boolean>> {
    this.ensureInitialized();

    const ttl = options?.ttl || this.config.defaultTTL;
    const adapters = options?.skipFallbacks ? [this.primaryAdapter].filter(Boolean) : this.getAvailableAdapters();
    const result = new Map<string, boolean>();

    // Initialize all keys as failed
    items.forEach((_, key) => result.set(key, false));

    for (const adapter of adapters) {
      try {
        const adapterResult = await adapter.setMany(items, ttl);
        adapterResult.forEach((success, key) => {
          if (success) {
            result.set(key, true);
          }
        });
      } catch (error) {
        this.emit('adapter-error', { adapter: adapter.type, error, operation: 'setMany' });
        continue;
      }
    }

    return result;
  }

  async deleteMany(keys: string[]): Promise<Map<string, boolean>> {
    this.ensureInitialized();

    const result = new Map<string, boolean>();
    const adapters = this.getAvailableAdapters();

    // Initialize all keys as failed
    keys.forEach((key) => result.set(key, false));

    for (const adapter of adapters) {
      try {
        const adapterResult = await adapter.deleteMany(keys);
        adapterResult.forEach((success, key) => {
          if (success) {
            result.set(key, true);
          }
        });
      } catch (error) {
        this.emit('adapter-error', { adapter: adapter.type, error, operation: 'deleteMany' });
        continue;
      }
    }

    return result;
  }

  // Statistics and management
  async getStats(): Promise<StorageManagerStats> {
    this.ensureInitialized();

    const primaryStats = this.primaryAdapter ? await this.primaryAdapter.stats() : null;
    const fallbackStats = await Promise.all(this.fallbackAdapters.map(async (adapter) => await adapter.stats()));

    return {
      primary: primaryStats
        ? {
            type: primaryStats.type,
            available: primaryStats.available,
            size: primaryStats.size,
            itemCount: primaryStats.itemCount,
          }
        : {
            type: this.config.primaryAdapter,
            available: false,
            size: 0,
            itemCount: 0,
          },
      fallbacks: fallbackStats.map((stats) => ({
        type: stats.type,
        available: stats.available,
        size: stats.size,
        itemCount: stats.itemCount,
      })),
      totalSize: (primaryStats?.size || 0) + fallbackStats.reduce((sum, stats) => sum + stats.size, 0),
      totalItems: (primaryStats?.itemCount || 0) + fallbackStats.reduce((sum, stats) => sum + stats.itemCount, 0),
    };
  }

  // Content change notifications for external contexts
  onChange(callback: (event: { type: 'set' | 'delete' | 'clear'; key?: string; value?: unknown }) => void): () => void {
    const handler = (event: StorageAdapterEvent) => {
      callback({
        type: event.type as 'set' | 'delete' | 'clear',
        key: event.key,
        value: event.value,
      });
    };

    this.on('stored', handler);
    this.on('deleted', handler);
    this.on('cleared', handler);

    // Return unsubscribe function
    return () => {
      this.off('stored', handler);
      this.off('deleted', handler);
      this.off('cleared', handler);
    };
  }

  // Private methods
  private async initializePrimaryAdapter(): Promise<void> {
    const config: StorageAdapterConfig = {
      type: this.config.primaryAdapter,
      name: this.config.namespace,
      ttl: this.config.defaultTTL,
      compression: this.config.compressionThreshold ? true : false,
      encryption: this.config.encryptionEnabled,
    };

    try {
      this.primaryAdapter = await StorageAdapterFactory.create(config);
      this.setupAdapterEventHandlers(this.primaryAdapter);
    } catch (error) {
      this.emit('adapter-init-failed', { type: this.config.primaryAdapter, error });
      throw new Error(`Failed to initialize primary adapter ${this.config.primaryAdapter}: ${error}`);
    }
  }

  private async initializeFallbackAdapters(): Promise<void> {
    if (!this.config.fallbackAdapters) return;

    for (const adapterType of this.config.fallbackAdapters) {
      if (adapterType === this.config.primaryAdapter) continue;

      const config: StorageAdapterConfig = {
        type: adapterType,
        name: `${this.config.namespace}-fallback`,
        ttl: this.config.defaultTTL,
        compression: false, // Disable compression for fallbacks to save CPU
        encryption: false,
      };

      try {
        const adapter = await StorageAdapterFactory.create(config);
        this.setupAdapterEventHandlers(adapter);
        this.fallbackAdapters.push(adapter);
      } catch (error) {
        this.emit('adapter-init-failed', { type: adapterType, error });
      }
    }
  }

  private setupAdapterEventHandlers(adapter: ClientStorageAdapter): void {
    adapter.onEvent((event) => {
      this.emit('adapter-event', { adapter: adapter.type, event });

      if (event.type === 'quota-exceeded') {
        this.emit('quota-exceeded', { adapter: adapter.type });
        this.handleQuotaExceeded(adapter);
      }
    });
  }

  private async handleQuotaExceeded(adapter: ClientStorageAdapter): Promise<void> {
    // Simple LRU eviction strategy
    try {
      const keys = await adapter.keys();
      // Remove 25% of items (this is a simple strategy, could be improved)
      const toRemove = keys.slice(0, Math.floor(keys.length * 0.25));
      await adapter.deleteMany(toRemove);
      this.emit('quota-cleanup', { adapter: adapter.type, removedCount: toRemove.length });
    } catch (error) {
      this.emit('quota-cleanup-failed', { adapter: adapter.type, error });
    }
  }

  private async migrateData(): Promise<void> {
    if (!this.primaryAdapter || this.fallbackAdapters.length === 0) return;

    // Migrate data from fallbacks to primary if primary is available
    for (const fallbackAdapter of this.fallbackAdapters) {
      try {
        const keys = await fallbackAdapter.keys();
        const data = await fallbackAdapter.getMany(keys);

        if (data.size > 0) {
          await this.primaryAdapter.setMany(data);
          this.emit('migration', {
            from: fallbackAdapter.type,
            to: this.primaryAdapter.type,
            itemCount: data.size,
          });
        }
      } catch (error) {
        this.emit('migration-failed', {
          from: fallbackAdapter.type,
          to: this.primaryAdapter.type,
          error,
        });
      }
    }
  }

  private getAvailableAdapters(): ClientStorageAdapter[] {
    const adapters: ClientStorageAdapter[] = [];

    if (this.primaryAdapter) {
      adapters.push(this.primaryAdapter);
    }

    adapters.push(...this.fallbackAdapters);

    return adapters;
  }

  private async setInBackground<T>(key: string, value: T): Promise<void> {
    // Set value in primary adapter in background without waiting
    if (this.primaryAdapter) {
      this.primaryAdapter.set(key, value).catch((error) => {
        this.emit('background-set-failed', { key, error });
      });
    }
  }

  private ensureInitialized(): void {
    if (!this.initialized) {
      throw new Error('StorageManager is not initialized. Call init() first.');
    }
  }
}
