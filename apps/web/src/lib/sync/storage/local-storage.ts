import { BaseClientStorageAdapter } from './base-storage';
import { StorageAdapterConfig, StorageItem } from './storage-types';

/**
 * LocalStorage adapter implementation
 */
export class LocalStorageAdapter extends BaseClientStorageAdapter {
  constructor(config: Omit<StorageAdapterConfig, 'type'>) {
    super({ ...config, type: 'localStorage' });
  }

  protected async _init(): Promise<void> {
    // No initialization needed for localStorage
  }

  protected async _destroy(): Promise<void> {
    // No cleanup needed for localStorage
  }

  protected async _isAvailable(): Promise<boolean> {
    try {
      const testKey = '__storage_test__';
      localStorage.setItem(testKey, 'test');
      localStorage.removeItem(testKey);
      return true;
    } catch {
      return false;
    }
  }

  protected async _get<T = unknown>(key: string): Promise<StorageItem<T> | null> {
    try {
      const item = localStorage.getItem(this.getStorageKey(key));
      return item ? JSON.parse(item) : null;
    } catch {
      return null;
    }
  }

  protected async _set<T = unknown>(key: string, item: StorageItem<T>): Promise<void> {
    const serialized = JSON.stringify(item);
    localStorage.setItem(this.getStorageKey(key), serialized);
  }

  protected async _delete(key: string): Promise<boolean> {
    const storageKey = this.getStorageKey(key);
    const existed = localStorage.getItem(storageKey) !== null;
    localStorage.removeItem(storageKey);
    return existed;
  }

  protected async _clear(): Promise<void> {
    const prefix = this.getStorageKey('');
    const keysToRemove: string[] = [];
    
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith(prefix)) {
        keysToRemove.push(key);
      }
    }
    
    keysToRemove.forEach((key) => localStorage.removeItem(key));
  }

  protected async _has(key: string): Promise<boolean> {
    return localStorage.getItem(this.getStorageKey(key)) !== null;
  }

  protected async _keys(): Promise<string[]> {
    const prefix = this.getStorageKey('');
    const keys: string[] = [];
    
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith(prefix)) {
        keys.push(key.slice(prefix.length));
      }
    }
    
    return keys;
  }

  protected async _size(): Promise<number> {
    const keys = await this._keys();
    let totalSize = 0;
    
    keys.forEach((key) => {
      const item = localStorage.getItem(this.getStorageKey(key));
      if (item) {
        totalSize += item.length * 2; // Each character is 2 bytes in UTF-16
      }
    });
    
    return totalSize;
  }

  private getStorageKey(key: string): string {
    return this.config.name ? `${this.config.name}:${key}` : key;
  }
}
