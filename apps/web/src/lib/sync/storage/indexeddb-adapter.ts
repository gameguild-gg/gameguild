import { BaseClientStorageAdapter } from './base-client-storage';
import { StorageAdapterConfig, StorageItem } from './storage-types';

/**
 * IndexedDB adapter implementation
 */
export class IndexedDBAdapter extends BaseClientStorageAdapter {
  private db: IDBDatabase | null = null;
  private readonly dbName: string;
  private readonly storeName = 'storage';

  constructor(config: Omit<StorageAdapterConfig, 'type'>) {
    super({ ...config, type: 'indexedDB' });
    this.dbName = config.name || 'default-storage';
  }

  protected async _init(): Promise<void> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.dbName, this.config.version || 1);

      request.onerror = () => reject(request.error);
      request.onsuccess = () => {
        this.db = request.result;
        resolve();
      };

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result;
        
        if (!db.objectStoreNames.contains(this.storeName)) {
          const store = db.createObjectStore(this.storeName, { keyPath: 'key' });
          store.createIndex('timestamp', 'timestamp', { unique: false });
          store.createIndex('ttl', 'ttl', { unique: false });
        }
      };
    });
  }

  protected async _destroy(): Promise<void> {
    if (this.db) {
      this.db.close();
      this.db = null;
    }

    return new Promise((resolve, reject) => {
      const deleteRequest = indexedDB.deleteDatabase(this.dbName);
      deleteRequest.onsuccess = () => resolve();
      deleteRequest.onerror = () => reject(deleteRequest.error);
    });
  }

  protected async _isAvailable(): Promise<boolean> {
    try {
      return typeof indexedDB !== 'undefined';
    } catch {
      return false;
    }
  }

  protected async _get<T = unknown>(key: string): Promise<StorageItem<T> | null> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.storeName], 'readonly');
      const store = transaction.objectStore(this.storeName);
      const request = store.get(key);

      request.onsuccess = () => {
        const result = request.result;
        resolve(result ? result.item : null);
      };
      request.onerror = () => reject(request.error);
    });
  }

  protected async _set<T = unknown>(key: string, item: StorageItem<T>): Promise<void> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.storeName], 'readwrite');
      const store = transaction.objectStore(this.storeName);
      const request = store.put({
        key,
        item,
        timestamp: item.timestamp,
        ttl: item.ttl,
      });

      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  protected async _delete(key: string): Promise<boolean> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.storeName], 'readwrite');
      const store = transaction.objectStore(this.storeName);
      const getRequest = store.get(key);

      getRequest.onsuccess = () => {
        const existed = getRequest.result !== undefined;
        if (existed) {
          const deleteRequest = store.delete(key);
          deleteRequest.onsuccess = () => resolve(true);
          deleteRequest.onerror = () => reject(deleteRequest.error);
        } else {
          resolve(false);
        }
      };
      getRequest.onerror = () => reject(getRequest.error);
    });
  }

  protected async _clear(): Promise<void> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.storeName], 'readwrite');
      const store = transaction.objectStore(this.storeName);
      const request = store.clear();

      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  protected async _has(key: string): Promise<boolean> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.storeName], 'readonly');
      const store = transaction.objectStore(this.storeName);
      const request = store.count(key);

      request.onsuccess = () => resolve(request.result > 0);
      request.onerror = () => reject(request.error);
    });
  }

  protected async _keys(): Promise<string[]> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.storeName], 'readonly');
      const store = transaction.objectStore(this.storeName);
      const request = store.getAllKeys();

      request.onsuccess = () => resolve(request.result as string[]);
      request.onerror = () => reject(request.error);
    });
  }

  protected async _size(): Promise<number> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve, reject) => {
      const transaction = this.db!.transaction([this.storeName], 'readonly');
      const store = transaction.objectStore(this.storeName);
      const request = store.getAll();

      request.onsuccess = () => {
        const totalSize = request.result.reduce((size, record) => {
          const serialized = JSON.stringify(record);
          return size + serialized.length * 2; // UTF-16 bytes
        }, 0);
        resolve(totalSize);
      };
      request.onerror = () => reject(request.error);
    });
  }

  // Optimized batch operations for IndexedDB
  async getMany<T = unknown>(keys: string[]): Promise<Map<string, T>> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve, reject) => {
      const result = new Map<string, T>();
      const transaction = this.db!.transaction([this.storeName], 'readonly');
      const store = transaction.objectStore(this.storeName);

      let completed = 0;
      
      keys.forEach((key) => {
        const request = store.get(key);
        request.onsuccess = () => {
          if (request.result?.item) {
            const item = request.result.item as StorageItem<T>;
            
            // Check TTL
            if (!item.ttl || Date.now() <= item.timestamp + item.ttl) {
              result.set(key, item.value);
            }
          }
          
          completed++;
          if (completed === keys.length) {
            resolve(result);
          }
        };
        request.onerror = () => reject(request.error);
      });
    });
  }

  async setMany<T = unknown>(items: Map<string, T>, ttl?: number): Promise<Map<string, boolean>> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve) => {
      const result = new Map<string, boolean>();
      const transaction = this.db!.transaction([this.storeName], 'readwrite');
      const store = transaction.objectStore(this.storeName);

      let completed = 0;
      const itemsArray = Array.from(items.entries());

      itemsArray.forEach(([key, value]) => {
        const item: StorageItem<T> = {
          value,
          timestamp: Date.now(),
          ttl: ttl || this.config.ttl,
          version: 1,
          compressed: this.config.compression || false,
          encrypted: this.config.encryption || false,
        };

        const request = store.put({
          key,
          item,
          timestamp: item.timestamp,
          ttl: item.ttl,
        });

        request.onsuccess = () => {
          result.set(key, true);
          completed++;
          if (completed === itemsArray.length) {
            resolve(result);
          }
        };
        
        request.onerror = () => {
          result.set(key, false);
          completed++;
          if (completed === itemsArray.length) {
            resolve(result);
          }
        };
      });
    });
  }

  async deleteMany(keys: string[]): Promise<Map<string, boolean>> {
    if (!this.db) throw new Error('Database not initialized');

    return new Promise((resolve) => {
      const result = new Map<string, boolean>();
      const transaction = this.db!.transaction([this.storeName], 'readwrite');
      const store = transaction.objectStore(this.storeName);

      let completed = 0;

      keys.forEach((key) => {
        const request = store.delete(key);
        request.onsuccess = () => {
          result.set(key, true);
          completed++;
          if (completed === keys.length) {
            resolve(result);
          }
        };
        request.onerror = () => {
          result.set(key, false);
          completed++;
          if (completed === keys.length) {
            resolve(result);
          }
        };
      });
    });
  }
}
