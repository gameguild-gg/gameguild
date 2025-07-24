import { BaseClientStorageAdapter } from './base-storage';
import { StorageAdapterConfig, StorageItem } from './storage-types';

/**
 * Cache API adapter implementation
 */
export class CacheAPIAdapter extends BaseClientStorageAdapter {
  private cache: Cache | null = null;
  private readonly cacheName: string;

  constructor(config: Omit<StorageAdapterConfig, 'type'>) {
    super({ ...config, type: 'cache' });
    this.cacheName = config.name || 'default-cache';
  }

  protected async _init(): Promise<void> {
    this.cache = await caches.open(this.cacheName);
  }

  protected async _destroy(): Promise<void> {
    if (this.cache) {
      await caches.delete(this.cacheName);
      this.cache = null;
    }
  }

  protected async _isAvailable(): Promise<boolean> {
    try {
      return typeof caches !== 'undefined';
    } catch {
      return false;
    }
  }

  protected async _get<T = unknown>(key: string): Promise<StorageItem<T> | null> {
    if (!this.cache) throw new Error('Cache not initialized');

    const response = await this.cache.match(this.getRequestUrl(key));
    if (!response) return null;

    try {
      const item: StorageItem<T> = await response.json();
      return item;
    } catch {
      return null;
    }
  }

  protected async _set<T = unknown>(key: string, item: StorageItem<T>): Promise<void> {
    if (!this.cache) throw new Error('Cache not initialized');

    const request = new Request(this.getRequestUrl(key));
    const response = new Response(JSON.stringify(item), {
      headers: {
        'Content-Type': 'application/json',
        'Cache-Control': item.ttl ? `max-age=${Math.floor(item.ttl / 1000)}` : 'no-cache',
      },
    });

    await this.cache.put(request, response);
  }

  protected async _delete(key: string): Promise<boolean> {
    if (!this.cache) throw new Error('Cache not initialized');

    const url = this.getRequestUrl(key);
    const existed = await this.cache.match(url);
    const deleted = await this.cache.delete(url);
    return existed !== undefined && deleted;
  }

  protected async _clear(): Promise<void> {
    if (!this.cache) throw new Error('Cache not initialized');

    const keys = await this.cache.keys();
    const prefix = this.getRequestUrl('');
    
    const deletePromises = keys.filter((request) => request.url.startsWith(prefix)).map((request) => this.cache!.delete(request));

    await Promise.all(deletePromises);
  }

  protected async _has(key: string): Promise<boolean> {
    if (!this.cache) throw new Error('Cache not initialized');

    const response = await this.cache.match(this.getRequestUrl(key));
    return response !== undefined;
  }

  protected async _keys(): Promise<string[]> {
    if (!this.cache) throw new Error('Cache not initialized');

    const requests = await this.cache.keys();
    const prefix = this.getRequestUrl('');
    
    return requests.filter((request) => request.url.startsWith(prefix)).map((request) => this.extractKeyFromUrl(request.url));
  }

  protected async _size(): Promise<number> {
    if (!this.cache) throw new Error('Cache not initialized');

    const requests = await this.cache.keys();
    const prefix = this.getRequestUrl('');
    let totalSize = 0;

    const sizePromises = requests
      .filter((request) => request.url.startsWith(prefix))
      .map(async (request) => {
        const response = await this.cache!.match(request);
        if (response) {
          const text = await response.text();
          return text.length * 2; // UTF-16 bytes
        }
        return 0;
      });

    const sizes = await Promise.all(sizePromises);
    totalSize = sizes.reduce((sum, size) => sum + size, 0);

    return totalSize;
  }

  // Optimized batch operations for Cache API
  async getMany<T = unknown>(keys: string[]): Promise<Map<string, T>> {
    if (!this.cache) throw new Error('Cache not initialized');

    const result = new Map<string, T>();

    const promises = keys.map(async (key) => {
      const response = await this.cache!.match(this.getRequestUrl(key));
      if (response) {
        try {
          const item: StorageItem<T> = await response.json();
          
          // Check TTL
          if (!item.ttl || Date.now() <= item.timestamp + item.ttl) {
            result.set(key, item.value);
          }
        } catch {
          // Ignore parsing errors
        }
      }
    });

    await Promise.all(promises);
    return result;
  }

  async setMany<T = unknown>(items: Map<string, T>, ttl?: number): Promise<Map<string, boolean>> {
    if (!this.cache) throw new Error('Cache not initialized');

    const result = new Map<string, boolean>();

    const promises = Array.from(items.entries()).map(async ([key, value]) => {
      try {
        const item: StorageItem<T> = {
          value,
          timestamp: Date.now(),
          ttl: ttl || this.config.ttl,
          version: 1,
          compressed: this.config.compression || false,
          encrypted: this.config.encryption || false,
        };

        const request = new Request(this.getRequestUrl(key));
        const response = new Response(JSON.stringify(item), {
          headers: {
            'Content-Type': 'application/json',
            'Cache-Control': item.ttl ? `max-age=${Math.floor(item.ttl / 1000)}` : 'no-cache',
          },
        });

        await this.cache!.put(request, response);
        result.set(key, true);
      } catch {
        result.set(key, false);
      }
    });

    await Promise.all(promises);
    return result;
  }

  async deleteMany(keys: string[]): Promise<Map<string, boolean>> {
    if (!this.cache) throw new Error('Cache not initialized');

    const result = new Map<string, boolean>();

    const promises = keys.map(async (key) => {
      try {
        const deleted = await this.cache!.delete(this.getRequestUrl(key));
        result.set(key, deleted);
      } catch {
        result.set(key, false);
      }
    });

    await Promise.all(promises);
    return result;
  }

  private getRequestUrl(key: string): string {
    return `https://storage.local/${this.cacheName}/${encodeURIComponent(key)}`;
  }

  private extractKeyFromUrl(url: string): string {
    const parts = url.split('/');
    const encodedKey = parts[parts.length - 1];
    return decodeURIComponent(encodedKey);
  }
}
