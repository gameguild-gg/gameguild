import { Repository, StorageAdapter, StorageOptions } from './storage.types';

import { MarkdownStorageAdapter } from './markdown-storage.adapter';
import { ImageStorageAdapter } from '@/lib/sync/storage/image-storage.adapter';
// import { MarkdownStorageAdapter } from './markdown-storage.adapter';

// import { ImageStorageAdapter } from './image-storage.adapter';

// Storage type registry
type StorageType = 'markdown' | 'image' | 'json' | 'binary';

export interface StorageConfig {
  type: StorageType;
  options: StorageOptions;
}

// Factory for creating storage adapters
export class StorageFactory {
  private static adapters = new Map<string, StorageAdapter>();

  static register(name: string, adapter: StorageAdapter): void {
    this.adapters.set(name, adapter);
  }

  static create(type: StorageType, options: StorageOptions = {}): StorageAdapter {
    // Return an existing adapter if already created (a singleton pattern)
    const key = `${type}:${JSON.stringify(options)}`;

    if (this.adapters.has(key)) {
      return this.adapters.get(key)!;
    }

    let adapter: StorageAdapter;

    switch (type) {
      case 'markdown':
        adapter = new MarkdownStorageAdapter(options.path);
        break;

      case 'image':
        adapter = new ImageStorageAdapter({
          basePath: options.path,
          publicUrl: options.publicUrl,
        });
        break;

      case 'json':
        // Could add JSON storage adapter
        throw new Error('JSON storage not yet implemented');

      case 'binary':
        // Could add a binary storage adapter
        throw new Error('Binary storage not yet implemented');

      default:
        throw new Error(`Unknown storage type: ${type}`);
    }

    this.adapters.set(key, adapter);
    return adapter;
  }

  static get(name: string): StorageAdapter | undefined {
    return this.adapters.get(name);
  }

  static clear(): void {
    this.adapters.clear();
  }
}

// Generic repository implementation with O(1) operations
export class StorageRepository<T extends { id?: string; slug?: string }> implements Repository<T> {
  public constructor(
    protected adapter: StorageAdapter<T>,
    private generateId: () => string = () => Date.now().toString(),
  ) {}

  // O(1) - Direct adapter call
  public async findById(id: string): Promise<T | null> {
    return this.adapter.read(id);
  }

  // O(1) - For adapters that support slug lookup
  async findBySlug(slug: string): Promise<T | null> {
    if ('findBySlug' in this.adapter) {
      return (this.adapter as { findBySlug: (slug: string) => Promise<T | null> }).findBySlug(slug);
    }

    const all = await this.findAll();
    return all.find((item) => item.slug === slug) || null;
  }

  // O(n) but optimized with the adapter's list method
  public async findAll(options?: { type?: string; limit?: number; offset?: number }): Promise<T[]> {
    const metadata = await this.adapter.list(undefined, options?.limit, options?.offset);

    // Batch read for efficiency
    if (this.adapter.batchRead) {
      const ids = metadata.map((metadata) => metadata.id);
      const results = await this.adapter.batchRead(ids);
      return Array.from(results.values());
    }

    // Fallback to individual reads
    const results = await Promise.all(metadata.map((metadata) => this.adapter.read(metadata.id)));

    return results.filter((result) => result !== null) as T[];
  }

  // O(n) but uses type index
  public async findByType(type: string): Promise<T[]> {
    const metadata = await this.adapter.list();
    const filtered = metadata.filter((metadata) => metadata.type === type);

    const ids = filtered.map((metadata) => metadata.id);

    if (this.adapter.batchRead) {
      const results = await this.adapter.batchRead(ids);
      return Array.from(results.values());
    }

    const results = await Promise.all(ids.map((id) => this.adapter.read(id)));

    return results.filter((result) => result !== null) as T[];
  }

  // O(n) but optimized with the date index
  async findByDateRange(start: Date, end: Date): Promise<T[]> {
    if ('findByDateRange' in this.adapter) {
      const ids = await (this.adapter as { findByDateRange: (start: Date, end: Date) => Promise<string[]> }).findByDateRange(start, end);

      if (this.adapter.batchRead) {
        const results = await this.adapter.batchRead(ids);
        return Array.from(results.values());
      }

      const results = await Promise.all(ids.map((id: string) => this.adapter.read(id)));
      return results.filter((r): r is T => r !== null);
    }

    const all = await this.findAll();
    return all.filter((item) => {
      const date = new Date((item as { createdAt?: string }).createdAt || '');
      return date >= start && date <= end;
    });
  }

  // O(1) write operations
  async create(data: Omit<T, 'id' | 'createdAt' | 'updatedAt'>): Promise<T> {
    const id = this.generateId();
    const now = new Date().toISOString();

    const fullData = {
      ...data,
      id,
      createdAt: now,
      updatedAt: now,
    };

    const result = await this.adapter.write(id, fullData);

    if (!result.success) throw result.error || new Error('Create failed');

    return result.data!;
  }

  async update(id: string, data: Partial<T>): Promise<T | null> {
    const existing = await this.adapter.read(id);
    if (!existing) return null;

    const updated = {
      ...existing,
      ...data,
      id, // Ensure ID doesn't change
      updatedAt: new Date().toISOString(),
    } as T;

    const result = await this.adapter.write(id, updated);

    if (!result.success) {
      throw result.error || new Error('Update failed');
    }

    return result.data!;
  }

  async delete(id: string): Promise<boolean> {
    return this.adapter.delete(id);
  }

  // Batch operations - optimized
  async updateMany(updates: Map<string, Partial<T>>): Promise<Map<string, T>> {
    const results = new Map<string, T>();

    // Read existing items
    const ids = Array.from(updates.keys());
    let existing: Map<string, T>;

    if (this.adapter.batchRead) {
      existing = await this.adapter.batchRead(ids);
    } else {
      existing = new Map();
      await Promise.all(
        ids.map(async (id) => {
          const item = await this.adapter.read(id);
          if (item) existing.set(id, item);
        }),
      );
    }

    // Merge updates
    const toWrite = new Map<string, T>();
    const now = new Date().toISOString();

    updates.forEach((update, id) => {
      const item = existing.get(id);
      if (item) {
        toWrite.set(id, {
          ...item,
          ...update,
          id,
          updatedAt: now,
        } as T);
      }
    });

    // Write updates
    if (this.adapter.batchWrite) {
      const writeResults = await this.adapter.batchWrite(toWrite);

      writeResults.forEach((result, id) => {
        if (result.success && result.data) {
          results.set(id, result.data);
        }
      });
    } else {
      await Promise.all(
        Array.from(toWrite.entries()).map(async ([id, data]) => {
          const result = await this.adapter.write(id, data);
          if (result.success && result.data) {
            results.set(id, result.data);
          }
        }),
      );
    }

    return results;
  }

  async deleteMany(ids: string[]): Promise<Map<string, boolean>> {
    if (this.adapter.batchDelete) return this.adapter.batchDelete(ids);

    // Fallback to individual deletes
    const results = new Map<string, boolean>();

    await Promise.all(
      ids.map(async (id) => {
        const result = await this.adapter.delete(id);
        results.set(id, result);
      }),
    );

    return results;
  }
}
