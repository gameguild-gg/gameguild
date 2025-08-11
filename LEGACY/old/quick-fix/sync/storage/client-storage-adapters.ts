import { BaseClientStorageAdapter } from './base-client-storage';
import { ClientStorageAdapter, StorageAdapterConfig, StorageAdapterFactory, StorageAdapterType, StorageItem } from './storage-types';

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

/**
 * SessionStorage adapter implementation
 */
export class SessionStorageAdapter extends BaseClientStorageAdapter {
  constructor(config: Omit<StorageAdapterConfig, 'type'>) {
    super({ ...config, type: 'sessionStorage' });
  }

  protected async _init(): Promise<void> {
    // No initialization needed for sessionStorage
  }

  protected async _destroy(): Promise<void> {
    // No cleanup needed for sessionStorage
  }

  protected async _isAvailable(): Promise<boolean> {
    try {
      const testKey = '__storage_test__';
      sessionStorage.setItem(testKey, 'test');
      sessionStorage.removeItem(testKey);
      return true;
    } catch {
      return false;
    }
  }

  protected async _get<T = unknown>(key: string): Promise<StorageItem<T> | null> {
    try {
      const item = sessionStorage.getItem(this.getStorageKey(key));
      return item ? JSON.parse(item) : null;
    } catch {
      return null;
    }
  }

  protected async _set<T = unknown>(key: string, item: StorageItem<T>): Promise<void> {
    const serialized = JSON.stringify(item);
    sessionStorage.setItem(this.getStorageKey(key), serialized);
  }

  protected async _delete(key: string): Promise<boolean> {
    const storageKey = this.getStorageKey(key);
    const existed = sessionStorage.getItem(storageKey) !== null;
    sessionStorage.removeItem(storageKey);
    return existed;
  }

  protected async _clear(): Promise<void> {
    const prefix = this.getStorageKey('');
    const keysToRemove: string[] = [];

    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i);
      if (key && key.startsWith(prefix)) {
        keysToRemove.push(key);
      }
    }

    keysToRemove.forEach((key) => sessionStorage.removeItem(key));
  }

  protected async _has(key: string): Promise<boolean> {
    return sessionStorage.getItem(this.getStorageKey(key)) !== null;
  }

  protected async _keys(): Promise<string[]> {
    const prefix = this.getStorageKey('');
    const keys: string[] = [];

    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i);
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
      const item = sessionStorage.getItem(this.getStorageKey(key));
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

/**
 * Client-side storage adapter factory
 */
export class ClientStorageFactory implements StorageAdapterFactory {
  private static adapters = new Map<string, ClientStorageAdapter>();

  static async create(config: StorageAdapterConfig): Promise<ClientStorageAdapter> {
    const key = `${config.type}:${JSON.stringify(config)}`;

    if (this.adapters.has(key)) {
      return this.adapters.get(key)!;
    }

    let adapter: ClientStorageAdapter;

    switch (config.type) {
      case 'localStorage':
        adapter = new LocalStorageAdapter(config);
        break;

      case 'sessionStorage':
        adapter = new SessionStorageAdapter(config);
        break;

      case 'indexedDB': {
        // Dynamically import IndexedDB adapter
        const { IndexedDBAdapter } = await import('./indexeddb-adapter');
        adapter = new IndexedDBAdapter(config);
        break;
      }

      case 'cache': {
        // Dynamically import Cache API adapter
        const { CacheAPIAdapter } = await import('./cache-adapter');
        adapter = new CacheAPIAdapter(config);
        break;
      }

      case 'memory':
        adapter = new MemoryAdapter(config);
        break;

      default:
        throw new Error(`Unknown storage adapter type: ${config.type}`);
    }

    await adapter.init();
    this.adapters.set(key, adapter);
    return adapter;
  }

  static isSupported(type: StorageAdapterType): boolean {
    switch (type) {
      case 'localStorage':
        try {
          return typeof localStorage !== 'undefined';
        } catch {
          return false;
        }

      case 'sessionStorage':
        try {
          return typeof sessionStorage !== 'undefined';
        } catch {
          return false;
        }

      case 'indexedDB':
        try {
          return typeof indexedDB !== 'undefined';
        } catch {
          return false;
        }

      case 'cache':
        try {
          return typeof caches !== 'undefined';
        } catch {
          return false;
        }

      case 'memory':
        return true;

      default:
        return false;
    }
  }

  static getAvailableTypes(): StorageAdapterType[] {
    const allTypes: StorageAdapterType[] = ['localStorage', 'sessionStorage', 'indexedDB', 'cache', 'memory'];
    return allTypes.filter((type) => this.isSupported(type));
  }

  static clear(): void {
    this.adapters.clear();
  }

  // Instance methods for factory interface
  async create(config: StorageAdapterConfig): Promise<ClientStorageAdapter> {
    return ClientStorageFactory.create(config);
  }

  isSupported(type: StorageAdapterType): boolean {
    return ClientStorageFactory.isSupported(type);
  }

  getAvailableTypes(): StorageAdapterType[] {
    return ClientStorageFactory.getAvailableTypes();
  }
}
