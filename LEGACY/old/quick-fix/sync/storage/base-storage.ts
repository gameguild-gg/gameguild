import { EventEmitter } from 'events';
import {
  ClientStorageAdapter,
  StorageAdapterConfig,
  StorageAdapterEvent,
  StorageAdapterEventHandler,
  StorageAdapterResult,
  StorageAdapterStats,
  StorageAdapterType,
  StorageItem,
} from './storage-types';

/**
 * Base abstract client-side storage adapter with common functionality
 */
export abstract class BaseClientStorageAdapter extends EventEmitter implements ClientStorageAdapter {
  public readonly type: StorageAdapterType;
  public readonly config: StorageAdapterConfig;
  protected initialized = false;

  constructor(config: StorageAdapterConfig) {
    super();
    this.type = config.type;
    this.config = { ...config };
  }

  // Abstract methods to be implemented by subclasses
  protected abstract _get<T = unknown>(key: string): Promise<StorageItem<T> | null>;
  protected abstract _set<T = unknown>(key: string, item: StorageItem<T>): Promise<void>;
  protected abstract _delete(key: string): Promise<boolean>;
  protected abstract _clear(): Promise<void>;
  protected abstract _has(key: string): Promise<boolean>;
  protected abstract _keys(): Promise<string[]>;
  protected abstract _size(): Promise<number>;
  protected abstract _init(): Promise<void>;
  protected abstract _destroy(): Promise<void>;
  protected abstract _isAvailable(): Promise<boolean>;

  async init(): Promise<void> {
    if (this.initialized) return;

    try {
      await this._init();
      this.initialized = true;
      this.emit('initialized');
    } catch (error) {
      this.emitEvent({
        type: 'error',
        adapter: this.type,
        timestamp: Date.now(),
        error: error instanceof Error ? error : new Error('Initialization failed'),
      });
      throw error;
    }
  }

  async destroy(): Promise<void> {
    try {
      await this._destroy();
      this.initialized = false;
      this.removeAllListeners();
    } catch (error) {
      this.emitEvent({
        type: 'error',
        adapter: this.type,
        timestamp: Date.now(),
        error: error instanceof Error ? error : new Error('Destruction failed'),
      });
      throw error;
    }
  }

  async isAvailable(): Promise<boolean> {
    try {
      return await this._isAvailable();
    } catch {
      return false;
    }
  }

  async get<T = unknown>(key: string): Promise<StorageAdapterResult<T>> {
    this.ensureInitialized();

    try {
      const item = await this._get<T>(key);

      if (!item) {
        return { success: false };
      }

      // Check TTL
      if (item.ttl && Date.now() > item.timestamp + item.ttl) {
        await this._delete(key);
        return { success: false };
      }

      return {
        success: true,
        data: item.value,
        fromCache: true,
      };
    } catch (error) {
      const errorObj = error instanceof Error ? error : new Error('Get operation failed');
      this.emitEvent({
        type: 'error',
        key,
        adapter: this.type,
        timestamp: Date.now(),
        error: errorObj,
      });

      return {
        success: false,
        error: errorObj,
      };
    }
  }

  async set<T = unknown>(key: string, value: T, ttl?: number): Promise<StorageAdapterResult<void>> {
    this.ensureInitialized();

    try {
      const item: StorageItem<T> = {
        value,
        timestamp: Date.now(),
        ttl: ttl || this.config.ttl,
        version: 1,
        compressed: this.config.compression || false,
        encrypted: this.config.encryption || false,
      };

      // Generate checksum if needed
      if (this.config.encryption || this.config.compression) {
        item.checksum = await this.generateChecksum(value);
      }

      await this._set(key, item);

      this.emitEvent({
        type: 'set',
        key,
        value,
        adapter: this.type,
        timestamp: Date.now(),
      });

      return { success: true };
    } catch (error) {
      const errorObj = error instanceof Error ? error : new Error('Set operation failed');

      // Handle quota exceeded error specifically
      if (this.isQuotaExceededError(error)) {
        this.emitEvent({
          type: 'quota-exceeded',
          key,
          adapter: this.type,
          timestamp: Date.now(),
          error: errorObj,
        });
      } else {
        this.emitEvent({
          type: 'error',
          key,
          adapter: this.type,
          timestamp: Date.now(),
          error: errorObj,
        });
      }

      return {
        success: false,
        error: errorObj,
      };
    }
  }

  async delete(key: string): Promise<StorageAdapterResult<boolean>> {
    this.ensureInitialized();

    try {
      const deleted = await this._delete(key);

      if (deleted) {
        this.emitEvent({
          type: 'delete',
          key,
          adapter: this.type,
          timestamp: Date.now(),
        });
      }

      return {
        success: true,
        data: deleted,
      };
    } catch (error) {
      const errorObj = error instanceof Error ? error : new Error('Delete operation failed');
      this.emitEvent({
        type: 'error',
        key,
        adapter: this.type,
        timestamp: Date.now(),
        error: errorObj,
      });

      return {
        success: false,
        error: errorObj,
      };
    }
  }

  async clear(): Promise<StorageAdapterResult<void>> {
    this.ensureInitialized();

    try {
      await this._clear();

      this.emitEvent({
        type: 'clear',
        adapter: this.type,
        timestamp: Date.now(),
      });

      return { success: true };
    } catch (error) {
      const errorObj = error instanceof Error ? error : new Error('Clear operation failed');
      this.emitEvent({
        type: 'error',
        adapter: this.type,
        timestamp: Date.now(),
        error: errorObj,
      });

      return {
        success: false,
        error: errorObj,
      };
    }
  }

  async has(key: string): Promise<boolean> {
    this.ensureInitialized();

    try {
      return await this._has(key);
    } catch {
      return false;
    }
  }

  async keys(): Promise<string[]> {
    this.ensureInitialized();

    try {
      return await this._keys();
    } catch {
      return [];
    }
  }

  async size(): Promise<number> {
    this.ensureInitialized();

    try {
      return await this._size();
    } catch {
      return 0;
    }
  }

  async stats(): Promise<StorageAdapterStats> {
    const size = await this.size();
    const keys = await this.keys();
    const available = await this.isAvailable();

    return {
      type: this.type,
      size,
      itemCount: keys.length,
      available,
      maxSize: this.config.maxSize,
      usedPercentage: this.config.maxSize ? (size / this.config.maxSize) * 100 : undefined,
    };
  }

  // Batch operations with default implementations
  async getMany<T = unknown>(keys: string[]): Promise<Map<string, T>> {
    const result = new Map<string, T>();

    await Promise.allSettled(
      keys.map(async (key) => {
        const item = await this.get<T>(key);
        if (item.success && item.data !== undefined) {
          result.set(key, item.data);
        }
      }),
    );

    return result;
  }

  async setMany<T = unknown>(items: Map<string, T>, ttl?: number): Promise<Map<string, boolean>> {
    const result = new Map<string, boolean>();

    await Promise.allSettled(
      Array.from(items.entries()).map(async ([key, value]) => {
        const setResult = await this.set(key, value, ttl);
        result.set(key, setResult.success);
      }),
    );

    return result;
  }

  async deleteMany(keys: string[]): Promise<Map<string, boolean>> {
    const result = new Map<string, boolean>();

    await Promise.allSettled(
      keys.map(async (key) => {
        const deleteResult = await this.delete(key);
        result.set(key, deleteResult.data || false);
      }),
    );

    return result;
  }

  // Event handling
  onEvent(handler: StorageAdapterEventHandler): void {
    this.on('storage-event', handler);
  }

  offEvent(handler: StorageAdapterEventHandler): void {
    this.off('storage-event', handler);
  }

  // Utility methods
  protected ensureInitialized(): void {
    if (!this.initialized) {
      throw new Error(`Storage adapter ${this.type} is not initialized`);
    }
  }

  protected emitEvent(event: StorageAdapterEvent): void {
    this.emit('storage-event', event);
  }

  protected isQuotaExceededError(error: unknown): boolean {
    if (error instanceof Error) {
      const message = error.message.toLowerCase();
      return message.includes('quota') || message.includes('storage') || message.includes('exceeded') || error.name === 'QuotaExceededError';
    }
    return false;
  }

  protected async generateChecksum(value: unknown): Promise<string> {
    const text = JSON.stringify(value);
    const encoder = new TextEncoder();
    const data = encoder.encode(text);

    if (typeof crypto !== 'undefined' && crypto.subtle) {
      const hashBuffer = await crypto.subtle.digest('SHA-256', data);
      const hashArray = Array.from(new Uint8Array(hashBuffer));
      return hashArray.map((b) => b.toString(16).padStart(2, '0')).join('');
    }

    // Fallback simple hash for environments without crypto.subtle
    let hash = 0;
    for (let i = 0; i < text.length; i++) {
      const char = text.charCodeAt(i);
      hash = (hash << 5) - hash + char;
      hash = hash & hash; // Convert to 32bit integer
    }
    return Math.abs(hash).toString(16);
  }
}
