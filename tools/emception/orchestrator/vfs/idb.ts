/**
 * IndexedDB-backed filesystem for persistent user files (/home).
 */

import { FSStats, IFileSystem } from './interface';

interface StoredEntry {
  path: string;
  data: Uint8Array;
  dir: boolean;
  mtime: number;
}

export class IDBFS implements IFileSystem {
  private dbName: string;
  private idb: IDBDatabase | null = null;
  private memCache = new Map<string, StoredEntry>();

  constructor(dbName: string) {
    this.dbName = dbName;
  }

  async init(): Promise<void> {
    this.idb = await new Promise((resolve, reject) => {
      const request = indexedDB.open(this.dbName, 1);
      request.onerror = () => reject(request.error);
      request.onsuccess = () => resolve(request.result);
      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result;
        if (!db.objectStoreNames.contains('files')) {
          db.createObjectStore('files', { keyPath: 'path' });
        }
      };
    });
  }

  private normalizePath(path: string): string {
    const parts = path.split('/').filter((p) => p && p !== '.');
    const result: string[] = [];
    for (const part of parts) {
      if (part === '..') result.pop();
      else result.push(part);
    }
    return '/' + result.join('/');
  }

  private async dbGet(path: string): Promise<StoredEntry | null> {
    if (!this.idb) return null;
    return new Promise((resolve) => {
      const tx = this.idb!.transaction('files', 'readonly');
      const request = tx.objectStore('files').get(path);
      request.onsuccess = () => resolve(request.result ?? null);
      request.onerror = () => resolve(null);
    });
  }

  private async dbPut(entry: StoredEntry): Promise<void> {
    if (!this.idb) return;
    return new Promise((resolve, reject) => {
      const tx = this.idb!.transaction('files', 'readwrite');
      const request = tx.objectStore('files').put(entry);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  private async dbDelete(path: string): Promise<void> {
    if (!this.idb) return;
    return new Promise((resolve, reject) => {
      const tx = this.idb!.transaction('files', 'readwrite');
      const request = tx.objectStore('files').delete(path);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }

  private async dbKeys(prefix: string): Promise<string[]> {
    if (!this.idb) return [];
    return new Promise((resolve) => {
      const tx = this.idb!.transaction('files', 'readonly');
      const request = tx.objectStore('files').openKeyCursor();
      const keys: string[] = [];
      const dirPrefix = prefix.endsWith('/') ? prefix : prefix + '/';

      request.onsuccess = () => {
        const cursor = request.result;
        if (!cursor) {
          resolve(keys);
          return;
        }
        const key = cursor.primaryKey as string;
        if (key === prefix || key.startsWith(dirPrefix)) {
          keys.push(key);
        }
        cursor.continue();
      };
      request.onerror = () => resolve([]);
    });
  }

  async readFile(path: string): Promise<Uint8Array | null> {
    const normalized = this.normalizePath(path);
    const cached = this.memCache.get(normalized);
    if (cached && !cached.dir) return cached.data;
    const entry = await this.dbGet(normalized);
    if (!entry || entry.dir) return null;
    this.memCache.set(normalized, entry);
    return entry.data;
  }

  async writeFile(path: string, data: Uint8Array): Promise<boolean> {
    const normalized = this.normalizePath(path);
    const dir = normalized.substring(0, normalized.lastIndexOf('/')) || '/';
    if (dir !== '/' && !(await this.exists(dir))) {
      await this.mkdir(dir);
    }
    const entry: StoredEntry = {
      path: normalized,
      data,
      dir: false,
      mtime: Date.now(),
    };
    await this.dbPut(entry);
    this.memCache.set(normalized, entry);
    return true;
  }

  async exists(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    if (this.memCache.has(normalized)) return true;
    const entry = await this.dbGet(normalized);
    return entry !== null;
  }

  async stat(path: string): Promise<FSStats | null> {
    const normalized = this.normalizePath(path);
    const entry = this.memCache.get(normalized) ?? (await this.dbGet(normalized));
    if (!entry) return null;
    return {
      type: entry.dir ? 'dir' : 'file',
      size: entry.data?.length ?? 0,
      mode: entry.dir ? 0o755 : 0o644,
      mtimeNs: BigInt(entry.mtime) * 1_000_000n,
    };
  }

  async readdir(path: string): Promise<string[]> {
    const normalized = this.normalizePath(path);
    const prefix = normalized === '/' ? '/' : normalized + '/';
    const keys = await this.dbKeys(prefix);
    const entries = new Set<string>();
    for (const key of keys) {
      if (key.startsWith(prefix)) {
        const rel = key.slice(prefix.length);
        const first = rel.split('/')[0];
        if (first) entries.add(first);
      }
    }
    return [...entries];
  }

  async deleteFile(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    const entry = await this.dbGet(normalized);
    if (!entry || entry.dir) return false;
    await this.dbDelete(normalized);
    this.memCache.delete(normalized);
    return true;
  }

  async mkdir(path: string): Promise<boolean> {
    const normalized = this.normalizePath(path);
    if (await this.exists(normalized)) return false;
    const dir = normalized.substring(0, normalized.lastIndexOf('/')) || '/';
    if (dir !== '/' && !(await this.exists(dir))) {
      await this.mkdir(dir);
    }
    const entry: StoredEntry = {
      path: normalized,
      data: new Uint8Array(0),
      dir: true,
      mtime: Date.now(),
    };
    await this.dbPut(entry);
    this.memCache.set(normalized, entry);
    return true;
  }
}
