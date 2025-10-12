import { BaseClientStorageAdapter } from './base-storage';
import { StorageAdapterConfig, StorageItem } from './storage-types';

/**
 * Memory adapter implementation (fallback or testing)
 */
export class MemoryAdapter extends BaseClientStorageAdapter {
  private storage = new Map<string, StorageItem>();

  constructor(config: Omit<StorageAdapterConfig, 'type'>) {
    super({ ...config, type: 'memory' });
  }

  protected async _init(): Promise<void> {
    // No initialization needed
  }

  protected async _destroy(): Promise<void> {
    this.storage.clear();
  }

  protected async _isAvailable(): Promise<boolean> {
    return true;
  }

  protected async _get<T = unknown>(key: string): Promise<StorageItem<T> | null> {
    const item = this.storage.get(key);
    return item ? (item as StorageItem<T>) : null;
  }

  protected async _set<T = unknown>(key: string, item: StorageItem<T>): Promise<void> {
    this.storage.set(key, item);
  }

  protected async _delete(key: string): Promise<boolean> {
    return this.storage.delete(key);
  }

  protected async _clear(): Promise<void> {
    this.storage.clear();
  }

  protected async _has(key: string): Promise<boolean> {
    return this.storage.has(key);
  }

  protected async _keys(): Promise<string[]> {
    return Array.from(this.storage.keys());
  }

  protected async _size(): Promise<number> {
    let totalSize = 0;
    this.storage.forEach((item) => {
      const serialized = JSON.stringify(item);
      totalSize += serialized.length * 2; // UTF-16 bytes
    });
    return totalSize;
  }
}
