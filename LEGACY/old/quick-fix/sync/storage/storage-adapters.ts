import { ClientStorageAdapter, StorageAdapterConfig, StorageAdapterType } from './storage-types';

/**
 * Client-side storage adapter factory
 */
export class StorageAdapterFactory {
  private static adapters = new Map<string, ClientStorageAdapter>();

  static async create(config: StorageAdapterConfig): Promise<ClientStorageAdapter> {
    const key = `${config.type}:${JSON.stringify(config)}`;

    if (this.adapters.has(key)) {
      return this.adapters.get(key)!;
    }

    let adapter: ClientStorageAdapter;

    switch (config.type) {
      case 'localStorage': {
        const { LocalStorageAdapter } = await import('./local-storage');
        adapter = new LocalStorageAdapter(config);
        break;
      }

      case 'sessionStorage': {
        const { SessionStorageAdapter } = await import('./session-storage');
        adapter = new SessionStorageAdapter(config);
        break;
      }

      case 'indexedDB': {
        const { IndexedDBAdapter } = await import('./indexed-db');
        adapter = new IndexedDBAdapter(config);
        break;
      }

      case 'cache': {
        const { CacheAPIAdapter } = await import('./cache-api');
        adapter = new CacheAPIAdapter(config);
        break;
      }

      case 'memory': {
        const { MemoryAdapter } = await import('./memory');
        adapter = new MemoryAdapter(config);
        break;
      }

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
    return StorageAdapterFactory.create(config);
  }

  isSupported(type: StorageAdapterType): boolean {
    return StorageAdapterFactory.isSupported(type);
  }

  getAvailableTypes(): StorageAdapterType[] {
    return StorageAdapterFactory.getAvailableTypes();
  }
}
